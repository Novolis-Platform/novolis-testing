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
    private readonly Stack<(string Heading, PlaywrightWalkthroughFlowKind Kind)> sections = new();
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

    /// <summary>Absolute HTML document path after <see cref="FlushAsync"/>.</summary>
    public string PlayerPath => Path.Combine(ArtifactDirectory, "walkthrough.html");

    /// <summary>Absolute Markdown story path after <see cref="FlushAsync"/>.</summary>
    public string MarkdownPath => Path.Combine(ArtifactDirectory, "walkthrough.md");

    /// <summary>Recorded steps captured so far.</summary>
    public IReadOnlyList<PlaywrightWalkthroughStep> Steps => steps;

    /// <summary>Outermost named section, or <c>Scenario</c> when none is open.</summary>
    public string CurrentPart =>
        CurrentSections.FirstOrDefault(heading => !string.IsNullOrWhiteSpace(heading)) ?? "Scenario";

    private IReadOnlyList<string> CurrentSections =>
        sections.Reverse().Select(section => section.Heading).ToArray();

    private string CurrentFlowName =>
        CurrentSections.LastOrDefault(heading => !string.IsNullOrWhiteSpace(heading)) ?? string.Empty;

    private string CurrentFlowPath =>
        string.Join(" · ", CurrentSections.Where(heading => !string.IsNullOrWhiteSpace(heading)));

    private string CurrentKindWire =>
        sections.Count == 0
            ? string.Empty
            : sections.Peek().Kind switch
            {
                PlaywrightWalkthroughFlowKind.Planned => "planned",
                PlaywrightWalkthroughFlowKind.Deviated => "deviated",
                PlaywrightWalkthroughFlowKind.Closed => "closed",
                _ => string.Empty,
            };

    /// <summary>Opens a top-level section. Later <see cref="BeginPart"/> calls replace it.</summary>
    /// <param name="name">Section heading.</param>
    public void BeginPart(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        sections.Clear();
        sections.Push((name, PlaywrightWalkthroughFlowKind.Default));
    }

    /// <summary>
    /// Opens a nestable section. A heading plus indent, or indent only when
    /// <paramref name="heading"/> is omitted. Dispose to leave the section.
    /// </summary>
    /// <param name="heading">Section title. Null or blank is a sectionless indent.</param>
    /// <param name="kind">Optional kind inherited by steps in this nest.</param>
    /// <returns>Scope that pops the section when disposed.</returns>
    public PlaywrightWalkthroughFlow BeginSection(
        string? heading = null,
        PlaywrightWalkthroughFlowKind kind = PlaywrightWalkthroughFlowKind.Default)
    {
        sections.Push((heading ?? string.Empty, kind));
        return new PlaywrightWalkthroughFlow(() =>
        {
            if (sections.Count > 0)
            {
                sections.Pop();
            }
        });
    }

    /// <summary>Opens a named nested section. Prefer <see cref="BeginSection"/>.</summary>
    /// <param name="name">Section heading.</param>
    /// <param name="kind">Optional kind inherited by steps in this nest.</param>
    /// <returns>Scope that pops the section when disposed.</returns>
    public PlaywrightWalkthroughFlow BeginFlow(
        string name,
        PlaywrightWalkthroughFlowKind kind = PlaywrightWalkthroughFlowKind.Default) =>
        BeginSection(name, kind);

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
            sections.Count,
            CurrentFlowName,
            CurrentFlowPath,
            CurrentKindWire,
            CurrentSections));
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
        var catalogRoot = PlaywrightArtifactStore.CatalogRootFrom(ArtifactDirectory);
        var indexHref = Path.GetRelativePath(ArtifactDirectory, Path.Combine(catalogRoot, PlaywrightWalkthroughCatalog.MarkdownFileName))
            .Replace('\\', '/');
        var htmlIndexHref = Path.GetRelativePath(ArtifactDirectory, Path.Combine(catalogRoot, PlaywrightWalkthroughCatalog.HtmlFileName))
            .Replace('\\', '/');
        PlaywrightWalkthroughCatalog.Synchronize(() =>
        {
            File.WriteAllText(ManifestPath, JsonSerializer.Serialize(manifest, JsonOptions));
            File.WriteAllText(MarkdownPath, PlaywrightWalkthroughMarkdown.Render(manifest, indexHref));
            File.WriteAllText(
                PlayerPath,
                PlaywrightWalkthroughPlayer.Render(
                    manifest,
                    htmlIndexHref,
                    relative => PlaywrightWalkthroughAssets.ToDataUriOrPath(ArtifactDirectory, relative)));
            PlaywrightWalkthroughCatalog.Refresh(catalogRoot);
        });
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
