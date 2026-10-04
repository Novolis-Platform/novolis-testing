using System.Text.Json;
using Microsoft.Playwright;

namespace Novolis.Testing.Playwright;

/// <summary>
/// Walkthrough recorder for a page owned by <c>TUnit.Playwright.PageTest</c>.
/// Does not launch Chromium — TUnit does that.
/// </summary>
public sealed class PlaywrightSession
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly List<PlaywrightWalkthroughStep> steps = [];
    private readonly Stack<(string Name, PlaywrightWalkthroughFlowKind Kind)> flows = new();
    private readonly PlaywrightSessionOptions options;

    /// <summary>Attaches to a TUnit-owned page.</summary>
    /// <param name="page">Page from <c>PageTest.Page</c>.</param>
    /// <param name="options">Walkthrough and artifact options.</param>
    public PlaywrightSession(IPage page, PlaywrightSessionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(page);
        Page = page;
        this.options = options ?? new PlaywrightSessionOptions();
        ArtifactDirectory = string.IsNullOrWhiteSpace(this.options.ArtifactDirectory)
            ? PlaywrightArtifactStore.ForCurrentTest()
            : this.options.ArtifactDirectory;
        Directory.CreateDirectory(ArtifactDirectory);
        Directory.CreateDirectory(Path.Combine(ArtifactDirectory, "frames"));
    }

    /// <summary>Active page for the test.</summary>
    public IPage Page { get; }

    /// <summary>Folder that receives frames and the walkthrough recording.</summary>
    public string ArtifactDirectory { get; }

    /// <summary>Absolute walkthrough JSON path after <see cref="FlushAsync"/>.</summary>
    public string ManifestPath => Path.Combine(ArtifactDirectory, "walkthrough.json");

    /// <summary>Absolute HTML player path after <see cref="FlushAsync"/>.</summary>
    public string PlayerPath => Path.Combine(ArtifactDirectory, "walkthrough.html");

    /// <summary>Recorded steps captured so far.</summary>
    public IReadOnlyList<PlaywrightWalkthroughStep> Steps => steps;

    /// <summary>Current scenario part. Steps inherit this until the next <see cref="BeginPart"/>.</summary>
    public string CurrentPart { get; private set; } = "Scenario";

    private string CurrentFlowName => flows.Count == 0 ? string.Empty : flows.Peek().Name;

    private string CurrentFlowPath =>
        flows.Count == 0
            ? string.Empty
            : string.Join(" · ", flows.Reverse().Select(flow => flow.Name));

    private string CurrentKindWire =>
        flows.Count == 0
            ? string.Empty
            : flows.Peek().Kind switch
            {
                PlaywrightWalkthroughFlowKind.Planned => "planned",
                PlaywrightWalkthroughFlowKind.Deviated => "deviated",
                PlaywrightWalkthroughFlowKind.Closed => "closed",
                _ => string.Empty,
            };

    /// <summary>Starts a named part. Later steps belong to it until the next part.</summary>
    /// <param name="name">Part title shown in the recording outline.</param>
    public void BeginPart(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        CurrentPart = name;
    }

    /// <summary>
    /// Opens a nested flow. Steps taken before dispose inherit depth, path, and kind
    /// so a Changed day or usual-hours save can show every screen, not one end frame.
    /// Flows stack; dispose in reverse order.
    /// </summary>
    /// <param name="name">Outline heading for this nest.</param>
    /// <param name="kind">Whether this nest followed the usual clock, left it, or was shut.</param>
    /// <returns>Scope that pops the flow when disposed.</returns>
    public PlaywrightWalkthroughFlow BeginFlow(
        string name,
        PlaywrightWalkthroughFlowKind kind = PlaywrightWalkthroughFlowKind.Default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        flows.Push((name, kind));
        return new PlaywrightWalkthroughFlow(() =>
        {
            if (flows.Count > 0)
            {
                flows.Pop();
            }
        });
    }

    /// <summary>
    /// Runs one named walkthrough step, then stores a PNG frame so the HTML player can replay it.
    /// </summary>
    /// <param name="name">Human-readable step title.</param>
    /// <param name="action">Work performed on <see cref="Page"/>.</param>
    /// <returns>A task that completes when the frame is written.</returns>
    public Task StepAsync(string name, Func<IPage, Task> action) =>
        StepAsync(name, string.Empty, action);

    /// <summary>
    /// Runs one named step with narration that the HTML recording shows beside the frame.
    /// </summary>
    /// <param name="name">Short step title.</param>
    /// <param name="narration">Why this step exists in the scenario.</param>
    /// <param name="action">Work performed on <see cref="Page"/>.</param>
    /// <returns>A task that completes when the frame is written.</returns>
    public async Task StepAsync(string name, string narration, Func<IPage, Task> action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(action);
        await action(Page);
        await CaptureAsync(name, narration);
    }

    /// <summary>Stores a screenshot frame without performing extra page work.</summary>
    /// <param name="name">Human-readable step title.</param>
    /// <returns>A task that completes when the frame is written.</returns>
    public Task CaptureAsync(string name) => CaptureAsync(name, string.Empty);

    /// <summary>Stores a screenshot frame with narration.</summary>
    /// <param name="name">Human-readable step title.</param>
    /// <param name="narration">Why this step exists in the scenario.</param>
    /// <returns>A task that completes when the frame is written.</returns>
    public async Task CaptureAsync(string name, string narration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var index = steps.Count + 1;
        var fileName = $"{index:00}-{PlaywrightArtifactStore.Sanitize(name)}.png";
        var path = Path.Combine(ArtifactDirectory, "frames", fileName);
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = path,
            FullPage = true,
        });
        steps.Add(new PlaywrightWalkthroughStep(
            index,
            CurrentPart,
            name,
            narration ?? string.Empty,
            $"frames/{fileName}",
            DateTimeOffset.UtcNow,
            flows.Count,
            CurrentFlowName,
            CurrentFlowPath,
            CurrentKindWire));
    }

    /// <summary>
    /// Writes the walkthrough JSON and HTML recording. Optionally notes a raw WebM when one exists.
    /// Does not close the TUnit browser context.
    /// </summary>
    /// <returns>A task that completes when artifacts are flushed.</returns>
    public async Task FlushAsync()
    {
        var rawCapture = await CopyRawCaptureAsync();
        var traceFile = File.Exists(Path.Combine(ArtifactDirectory, "trace.zip"))
            ? "trace.zip"
            : null;
        var manifest = new PlaywrightWalkthroughManifest(
            options.WalkthroughTitle,
            options.BaseUrl?.AbsoluteUri,
            ArtifactDirectory,
            rawCapture,
            traceFile,
            options.FrameHoldMilliseconds,
            steps);
        await File.WriteAllTextAsync(ManifestPath, JsonSerializer.Serialize(manifest, JsonOptions));
        await File.WriteAllTextAsync(PlayerPath, PlaywrightWalkthroughPlayer.Render(manifest));
    }

    private async Task<string?> CopyRawCaptureAsync()
    {
        if (!options.RecordVideo)
        {
            return null;
        }

        try
        {
            var recorded = Page.Video is null ? null : await Page.Video.PathAsync();
            if (IsUsableCapture(recorded))
            {
                return Path.GetFileName(recorded);
            }
        }
        catch (PlaywrightException)
        {
        }

        return Directory.Exists(ArtifactDirectory)
            ? Directory.GetFiles(ArtifactDirectory, "*.webm")
                .Where(IsUsableCapture)
                .Select(Path.GetFileName)
                .FirstOrDefault()
            : null;
    }

    private static bool IsUsableCapture(string? path) =>
        !string.IsNullOrWhiteSpace(path) && File.Exists(path) && new FileInfo(path).Length > 0;
}
