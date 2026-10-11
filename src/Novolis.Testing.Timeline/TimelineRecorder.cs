using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Novolis.Testing.Timeline;

/// <summary>
/// Collects image-backed states and supporting evidence, then writes portable
/// JSON, Markdown, and HTML timeline artifacts.
/// </summary>
public sealed class TimelineRecorder
{
    /// <summary>Default duration for each frame in the HTML player.</summary>
    public const int DefaultFrameHoldMilliseconds = 4000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly List<TimelineStep> _steps = [];
    private readonly List<TimelineEvent> _events = [];
    private readonly List<object> _telemetry = [];
    private readonly List<string> _logs = [];
    private readonly Stack<(string Heading, TimelineFlowKind Kind)> sections = new();
    private readonly TimelineRecorderOptions options;
    private string? rawCaptureFile;
    private string? traceFile;

    private TimelineRecorder(
        string artifactDirectory,
        string title,
        TimelineRecorderOptions options)
    {
        ArtifactDirectory = artifactDirectory;
        Title = title;
        this.options = options;
        LogFileName = options.LogFileName;
        Directory.CreateDirectory(artifactDirectory);
        Directory.CreateDirectory(Path.Combine(artifactDirectory, "frames"));
        Directory.CreateDirectory(Path.Combine(artifactDirectory, "logs"));
    }

    /// <summary>Creates a uniquely named recording beneath <paramref name="root"/>.</summary>
    public static TimelineRecorder Create(
        string root,
        string title,
        string logFileName = "timeline.log")
        => Create(
            root,
            title,
            new TimelineRecorderOptions
            {
                LogFileName = logFileName,
            });

    /// <summary>Creates a uniquely named recording with the supplied options.</summary>
    public static TimelineRecorder Create(
        string root,
        string title,
        TimelineRecorderOptions? options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        options ??= new TimelineRecorderOptions();
        ValidateLogFileName(options.LogFileName);

        var runId = DateTimeOffset.UtcNow.ToString(
            "yyyyMMddTHHmmssfffZ",
            CultureInfo.InvariantCulture)
            + "-"
            + Guid.NewGuid().ToString("N");
        var directory = Path.Combine(Path.GetFullPath(root), runId);
        return Open(directory, title, options);
    }

    /// <summary>Opens a recording in an existing artifact directory.</summary>
    public static TimelineRecorder Open(
        string artifactDirectory,
        string title,
        TimelineRecorderOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        options ??= new TimelineRecorderOptions();
        ValidateLogFileName(options.LogFileName);
        return new TimelineRecorder(
            Path.GetFullPath(artifactDirectory),
            title,
            options);
    }

    /// <summary>Directory containing this recording's artifacts.</summary>
    public string ArtifactDirectory { get; }

    /// <summary>Human-readable recording title.</summary>
    public string Title { get; }

    /// <summary>File name written beneath the recording's logs directory.</summary>
    public string LogFileName { get; }

    /// <summary>Events recorded so far, in recording order.</summary>
    public IReadOnlyList<TimelineEvent> Events => _events;

    /// <summary>Telemetry records recorded so far.</summary>
    public IReadOnlyList<object> Telemetry => _telemetry;

    /// <summary>Log lines recorded so far.</summary>
    public IReadOnlyList<string> Logs => _logs;

    /// <summary>Image-backed states recorded so far.</summary>
    public IReadOnlyList<TimelineStep> Steps => _steps;

    /// <summary>Outermost named section, or <c>Scenario</c> when none is open.</summary>
    public string CurrentPart =>
        CurrentSections.FirstOrDefault(heading => !string.IsNullOrWhiteSpace(heading))
        ?? "Scenario";

    /// <summary>Opens or replaces the top-level timeline section.</summary>
    public void BeginPart(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        sections.Clear();
        sections.Push((name, TimelineFlowKind.Default));
    }

    /// <summary>Opens a nestable timeline section.</summary>
    public TimelineScope BeginSection(
        string? heading = null,
        TimelineFlowKind kind = TimelineFlowKind.Default)
    {
        sections.Push((heading ?? string.Empty, kind));
        return new TimelineScope(() =>
        {
            if (sections.Count > 0)
            {
                sections.Pop();
            }
        });
    }

    /// <summary>Opens a named nested timeline section.</summary>
    public TimelineScope BeginFlow(
        string name,
        TimelineFlowKind kind = TimelineFlowKind.Default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return BeginSection(name, kind);
    }

    /// <summary>Associates a raw capture file with the recording.</summary>
    public void SetRawCapture(string? relativePath) =>
        rawCaptureFile = NormalizeArtifactPath(relativePath);

    /// <summary>Associates a trace file with the recording.</summary>
    public void SetTrace(string? relativePath) =>
        traceFile = NormalizeArtifactPath(relativePath);

    /// <summary>
    /// Captures one image-backed state. The callback writes the requested frame file.
    /// </summary>
    public async Task CaptureAsync(
        string name,
        string narration,
        Func<string, Task> captureFrame,
        IReadOnlyDictionary<string, object?>? metadata = null,
        string frameExtension = "png")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(narration);
        ArgumentNullException.ThrowIfNull(captureFrame);
        var extension = ValidateFrameExtension(frameExtension);

        var index = _steps.Count + 1;
        var fileName = $"{index:00}-{SanitizeName(name)}.{extension}";
        var relativePath = Path.Combine("frames", fileName);
        var absolutePath = Path.Combine(ArtifactDirectory, relativePath);
        await captureFrame(absolutePath).ConfigureAwait(false);

        var capturedAt = DateTimeOffset.UtcNow;
        _steps.Add(new TimelineStep(
            index,
            CurrentPart,
            name,
            narration,
            relativePath.Replace('\\', '/'),
            capturedAt,
            sections.Count,
            CurrentFlowName,
            CurrentFlowPath,
            CurrentKindWire,
            CurrentSections));
        _events.Add(new TimelineEvent(
            _events.Count + 1,
            capturedAt,
            name,
            "captured",
            narration,
            metadata));
    }

    /// <summary>Records a named outcome or transition.</summary>
    public void Record(
        string name,
        string status,
        string? detail = null,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        _events.Add(new TimelineEvent(
            _events.Count + 1,
            DateTimeOffset.UtcNow,
            name,
            status,
            detail,
            metadata));
    }

    /// <summary>Records a structured measurement or diagnostic value.</summary>
    public void RecordTelemetry(string name, object value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);
        _telemetry.Add(new
        {
            name,
            capturedAtUtc = DateTimeOffset.UtcNow,
            value,
        });
    }

    /// <summary>Adds a timestamped diagnostic line to the recording.</summary>
    public void Log(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _logs.Add($"{DateTimeOffset.UtcNow:O} {message}");
    }

    /// <summary>Writes all timeline artifacts to <see cref="ArtifactDirectory"/>.</summary>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        var manifest = new TimelineManifest(
            Title,
            options.BaseUrl,
            ArtifactDirectory,
            rawCaptureFile ?? NormalizeArtifactPath(options.RawCaptureFile),
            traceFile ?? NormalizeArtifactPath(options.TraceFile),
            options.FrameHoldMilliseconds > 0
                ? options.FrameHoldMilliseconds
                : DefaultFrameHoldMilliseconds,
            _steps,
            LogFileName);
        var document = new TimelineDocument(
            "novolis.testing.timeline/v1",
            DateTimeOffset.UtcNow,
            manifest,
            _events,
            _telemetry);

        await WriteAsync(
                "timeline.json",
                JsonSerializer.Serialize(document, JsonOptions),
                cancellationToken)
            .ConfigureAwait(false);
        await WriteAsync(
                "timeline.md",
                TimelineMarkdownRenderer.Render(manifest),
                cancellationToken)
            .ConfigureAwait(false);
        await WriteAsync(
                "timeline.html",
                TimelineHtmlRenderer.Render(
                    manifest,
                    _events,
                    _telemetry,
                    _logs,
                    relative => TimelineAssets.ToDataUriOrPath(
                        ArtifactDirectory,
                        relative)),
                cancellationToken)
            .ConfigureAwait(false);
        await WriteAsync(
                Path.Combine("logs", "events.ndjson"),
                string.Join(
                    Environment.NewLine,
                    _events.Select(item =>
                        JsonSerializer.Serialize(item, JsonOptions))),
                cancellationToken)
            .ConfigureAwait(false);
        await WriteAsync(
                Path.Combine("logs", LogFileName),
                string.Join(Environment.NewLine, _logs),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task WriteAsync(
        string relativePath,
        string content,
        CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(
                Path.Combine(ArtifactDirectory, relativePath),
                content,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private IReadOnlyList<string> CurrentSections =>
        sections.Reverse().Select(section => section.Heading).ToArray();

    private string CurrentFlowName =>
        CurrentSections.LastOrDefault(heading => !string.IsNullOrWhiteSpace(heading))
        ?? string.Empty;

    private string CurrentFlowPath =>
        string.Join(
            " · ",
            CurrentSections.Where(heading => !string.IsNullOrWhiteSpace(heading)));

    private string CurrentKindWire =>
        sections.Count == 0
            ? string.Empty
            : sections.Peek().Kind switch
            {
                TimelineFlowKind.Planned => "planned",
                TimelineFlowKind.Deviated => "deviated",
                TimelineFlowKind.Closed => "closed",
                _ => string.Empty,
            };

    private static string SanitizeName(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            builder.Append(
                char.IsLetterOrDigit(character) || character is '-' or '_'
                    ? character
                    : '_');
        }

        return builder.Length == 0 ? "frame" : builder.ToString();
    }

    private static string ValidateFrameExtension(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var extension = value.Trim().TrimStart('.');
        if (extension.Length == 0
            || extension.Any(character => !char.IsLetterOrDigit(character)))
        {
            throw new ArgumentException(
                "Frame extension must contain only letters and digits.",
                nameof(value));
        }

        return extension.ToLowerInvariant();
    }

    private static void ValidateLogFileName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (!string.Equals(Path.GetFileName(value), value, StringComparison.Ordinal)
            || !value.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Log file name must be a file name ending in .log.",
                nameof(value));
        }
    }

    private static string? NormalizeArtifactPath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var normalized = relativePath.Replace('\\', '/');
        if (Path.IsPathRooted(normalized)
            || normalized.Split('/').Any(segment => segment == ".."))
        {
            throw new ArgumentException(
                "Artifact paths must be relative to the timeline directory.",
                nameof(relativePath));
        }

        return normalized;
    }
}
