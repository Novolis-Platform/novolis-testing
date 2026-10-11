using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using Novolis.Testing.Timeline;

namespace Novolis.Testing.Appium;

/// <summary>
/// Per-test evidence timeline for a driver owned by an Appium session.
/// Does not connect to Appium and does not dispose the driver.
/// </summary>
public sealed class AppiumTimeline
{
    private readonly AppiumDriver? driver;
    private readonly TimelineRecorder timeline;
    private readonly Func<string, Task> captureFrame;

    /// <summary>Attaches a recording to an already connected driver.</summary>
    /// <param name="driver">Android or Windows driver for screenshots and step actions.</param>
    /// <param name="options">Timeline and artifact options.</param>
    public AppiumTimeline(AppiumDriver driver, AppiumTimelineOptions? options = null)
        : this(
            options ?? new AppiumTimelineOptions(),
            path => SaveScreenshotAsync(driver, path),
            driver ?? throw new ArgumentNullException(nameof(driver)))
    {
    }

    /// <summary>Opens a recording that stores frames through <paramref name="captureFrame"/>.</summary>
    /// <param name="options">Timeline and artifact options.</param>
    /// <param name="captureFrame">Writes one screenshot at the requested path.</param>
    internal AppiumTimeline(AppiumTimelineOptions options, Func<string, Task> captureFrame)
        : this(options, captureFrame, driver: null)
    {
    }

    private AppiumTimeline(
        AppiumTimelineOptions options,
        Func<string, Task> captureFrame,
        AppiumDriver? driver)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(captureFrame);
        this.driver = driver;
        this.captureFrame = captureFrame;
        ArtifactDirectory = string.IsNullOrWhiteSpace(options.ArtifactDirectory)
            ? AppiumArtifactStore.ForCurrentTest()
            : Path.GetFullPath(options.ArtifactDirectory);
        timeline = TimelineRecorder.Open(
            ArtifactDirectory,
            options.TimelineTitle,
            new TimelineRecorderOptions
            {
                FrameHoldMilliseconds = options.FrameHoldMilliseconds,
                LogFileName = "timeline.log",
            });
    }

    /// <summary>Driver this recording screenshots.</summary>
    public AppiumDriver Driver =>
        driver ?? throw new InvalidOperationException("Appium timeline has no driver.");

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

    /// <summary>Events recorded so far, in recording order.</summary>
    public IReadOnlyList<TimelineEvent> Events => timeline.Events;

    /// <summary>Outermost named section, or <c>Scenario</c> when none is open.</summary>
    public string CurrentPart => timeline.CurrentPart;

    /// <summary>Opens a top-level section. Later <see cref="BeginPart"/> calls replace it.</summary>
    /// <param name="name">Section heading.</param>
    public void BeginPart(string name) => timeline.BeginPart(name);

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
    /// Runs one named step, then stores a PNG frame so the HTML player can replay it.
    /// </summary>
    /// <param name="name">Human-readable step title. Also used as the narration.</param>
    /// <param name="action">Work performed on <see cref="Driver"/>.</param>
    /// <returns>A task that completes when the frame is written.</returns>
    public Task StepAsync(string name, Action<AppiumDriver> action) =>
        StepAsync(name, name, action);

    /// <summary>
    /// Runs one named step with narration that the HTML recording shows beside the frame.
    /// </summary>
    /// <param name="name">Short step title.</param>
    /// <param name="narration">Why this step exists in the scenario.</param>
    /// <param name="action">Work performed on <see cref="Driver"/>.</param>
    /// <returns>A task that completes when the frame is written.</returns>
    public Task StepAsync(string name, string narration, Action<AppiumDriver> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        action(Driver);
        return CaptureAsync(name, narration);
    }

    /// <summary>
    /// Runs one named asynchronous step, then stores a PNG frame.
    /// </summary>
    /// <param name="name">Human-readable step title. Also used as the narration.</param>
    /// <param name="action">Work performed on <see cref="Driver"/>.</param>
    /// <returns>A task that completes when the frame is written.</returns>
    public Task StepAsync(string name, Func<AppiumDriver, Task> action) =>
        StepAsync(name, name, action);

    /// <summary>
    /// Runs one named asynchronous step with narration, then stores a PNG frame.
    /// </summary>
    /// <param name="name">Short step title.</param>
    /// <param name="narration">Why this step exists in the scenario.</param>
    /// <param name="action">Work performed on <see cref="Driver"/>.</param>
    /// <returns>A task that completes when the frame is written.</returns>
    public async Task StepAsync(string name, string narration, Func<AppiumDriver, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        await action(Driver).ConfigureAwait(false);
        await CaptureAsync(name, narration).ConfigureAwait(false);
    }

    /// <summary>Stores a screenshot frame. The step title is the narration.</summary>
    /// <param name="name">Human-readable step title.</param>
    /// <returns>A task that completes when the frame is written.</returns>
    public Task CaptureAsync(string name) => CaptureAsync(name, name);

    /// <summary>Stores a screenshot frame with narration.</summary>
    /// <param name="name">Human-readable step title.</param>
    /// <param name="narration">Why this step exists in the scenario.</param>
    /// <param name="metadata">Optional values stored on the captured event.</param>
    /// <returns>A task that completes when the frame is written.</returns>
    public async Task CaptureAsync(
        string name,
        string narration,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(narration);
        await timeline.CaptureAsync(name, narration, captureFrame, metadata).ConfigureAwait(false);
    }

    /// <summary>Records a named outcome or transition without a frame.</summary>
    /// <param name="name">Event name.</param>
    /// <param name="status">Outcome such as <c>passed</c>, <c>skipped</c>, or <c>failed</c>.</param>
    /// <param name="detail">Optional explanation.</param>
    /// <param name="metadata">Optional values stored on the event.</param>
    public void Record(
        string name,
        string status,
        string? detail = null,
        IReadOnlyDictionary<string, object?>? metadata = null) =>
        timeline.Record(name, status, detail, metadata);

    /// <summary>Records a structured measurement.</summary>
    /// <param name="name">Measurement name.</param>
    /// <param name="value">Value written into the timeline document.</param>
    public void RecordTelemetry(string name, object value) =>
        timeline.RecordTelemetry(name, value);

    /// <summary>Adds a timestamped diagnostic line to the recording.</summary>
    /// <param name="message">Log line.</param>
    public void Log(string message) => timeline.Log(message);

    /// <summary>
    /// Writes the timeline JSON, Markdown, and HTML recording, then refreshes the collection index.
    /// Does not close the Appium driver.
    /// </summary>
    /// <param name="cancellationToken">Token observed while the artifact files are written.</param>
    /// <returns>A task that completes when artifacts are flushed.</returns>
    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        await timeline.FlushAsync(cancellationToken).ConfigureAwait(false);
        TimelineCatalog.Refresh(AppiumArtifactStore.CatalogRootFrom(ArtifactDirectory));
    }

    private static Task SaveScreenshotAsync(AppiumDriver driver, string path)
    {
        ((ITakesScreenshot)driver).GetScreenshot().SaveAsFile(path);
        return Task.CompletedTask;
    }
}
