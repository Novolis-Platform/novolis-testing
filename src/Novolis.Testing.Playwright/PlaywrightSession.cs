using Microsoft.Playwright;
using Novolis.Testing.Timeline;

namespace Novolis.Testing.Playwright;

/// <summary>
/// Timeline recorder for a page owned by <c>TUnit.Playwright.PageTest</c>.
/// Does not launch Chromium — TUnit does that.
/// </summary>
public sealed class PlaywrightSession
{
    private readonly TimelineRecorder timeline;
    private readonly PlaywrightSessionOptions options;

    /// <summary>Attaches to a TUnit-owned page.</summary>
    /// <param name="page">Page from <c>PageTest.Page</c>.</param>
    /// <param name="options">Timeline and artifact options.</param>
    public PlaywrightSession(IPage page, PlaywrightSessionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(page);
        Page = page;
        this.options = options ?? new PlaywrightSessionOptions();
        ArtifactDirectory = string.IsNullOrWhiteSpace(this.options.ArtifactDirectory)
            ? PlaywrightArtifactStore.ForCurrentTest()
            : this.options.ArtifactDirectory;
        timeline = TimelineRecorder.Open(
            ArtifactDirectory,
            this.options.TimelineTitle,
            new TimelineRecorderOptions
            {
                BaseUrl = this.options.BaseUrl?.AbsoluteUri,
                FrameHoldMilliseconds = this.options.FrameHoldMilliseconds,
                LogFileName = "timeline.log",
            });
    }

    /// <summary>Active page for the test.</summary>
    public IPage Page { get; }

    /// <summary>Folder that receives frames and the timeline recording.</summary>
    public string ArtifactDirectory { get; }

    /// <summary>Absolute timeline JSON path after <see cref="FlushAsync"/>.</summary>
    public string TimelinePath => Path.Combine(ArtifactDirectory, "timeline.json");

    /// <summary>Absolute HTML document path after <see cref="FlushAsync"/>.</summary>
    public string HtmlPath => Path.Combine(ArtifactDirectory, "timeline.html");

    /// <summary>Absolute Markdown story path after <see cref="FlushAsync"/>.</summary>
    public string MarkdownPath => Path.Combine(ArtifactDirectory, "timeline.md");

    /// <summary>Recorded timeline frames captured so far.</summary>
    public IReadOnlyList<TimelineStep> Steps => timeline.Steps;

    /// <summary>Outermost named section, or <c>Scenario</c> when none is open.</summary>
    public string CurrentPart => timeline.CurrentPart;

    /// <summary>Opens a top-level section. Later <see cref="BeginPart"/> calls replace it.</summary>
    /// <param name="name">Section heading.</param>
    public void BeginPart(string name)
        => timeline.BeginPart(name);

    /// <summary>
    /// Opens a nestable section. A heading plus indent, or indent only when
    /// <paramref name="heading"/> is omitted. Dispose to leave the section.
    /// </summary>
    /// <param name="heading">Section title. Null or blank is a sectionless indent.</param>
    /// <param name="kind">Optional kind inherited by steps in this nest.</param>
    /// <returns>Scope that pops the section when disposed.</returns>
    public TimelineScope BeginSection(
        string? heading = null,
        TimelineFlowKind kind = TimelineFlowKind.Default) =>
        timeline.BeginSection(heading, kind);

    /// <summary>Opens a named nested section. Prefer <see cref="BeginSection"/>.</summary>
    /// <param name="name">Section heading.</param>
    /// <param name="kind">Optional kind inherited by steps in this nest.</param>
    /// <returns>Scope that pops the section when disposed.</returns>
    public TimelineScope BeginFlow(
        string name,
        TimelineFlowKind kind = TimelineFlowKind.Default) =>
        timeline.BeginFlow(name, kind);

    /// <summary>
    /// Runs one named timeline step, then stores a PNG frame so the HTML player can replay it.
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
        await action(Page).ConfigureAwait(false);
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
        await timeline.CaptureAsync(
                name,
                narration ?? string.Empty,
                path => Page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = path,
                    FullPage = true,
                }))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Writes the timeline JSON, Markdown, and HTML recording. Optionally notes a raw WebM when one exists.
    /// Does not close the TUnit browser context.
    /// </summary>
    /// <returns>A task that completes when artifacts are flushed.</returns>
    public async Task FlushAsync()
    {
        var rawCapture = await CopyRawCaptureAsync();
        var traceFile = File.Exists(Path.Combine(ArtifactDirectory, "trace.zip"))
            ? "trace.zip"
            : null;
        timeline.SetRawCapture(rawCapture);
        timeline.SetTrace(traceFile);
        await timeline.FlushAsync().ConfigureAwait(false);
        var catalogRoot = PlaywrightArtifactStore.CatalogRootFrom(ArtifactDirectory);
        TimelineCatalog.Refresh(catalogRoot);
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
