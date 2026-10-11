using OpenQA.Selenium.Appium;
using Novolis.Testing.Timeline;
using TUnit.Core;

namespace Novolis.Testing.Appium;

/// <summary>
/// Per-test evidence timeline for a class-scoped Appium driver.
/// Android and Windows fixtures own the session; this type writes step frames for each test.
/// </summary>
public abstract class AppiumTimelineTestBase
{
    private AppiumTimeline? timeline;

    /// <summary>Recording attached by <see cref="AttachTimeline"/>.</summary>
    protected AppiumTimeline Timeline =>
        timeline ?? throw new InvalidOperationException("Appium timeline has not started.");

    /// <summary>Driver screenshotted for this test. The class fixture still owns the session.</summary>
    protected abstract AppiumDriver TimelineDriver { get; }

    /// <summary>
    /// Builds timeline options for the current test.
    /// Override and use <c>with</c> to change the title while keeping the stamp folder.
    /// </summary>
    /// <returns>Timeline options.</returns>
    protected virtual AppiumTimelineOptions CreateTimelineOptions() => new()
    {
        ArtifactDirectory = AppiumArtifactStore.ForCurrentTest(),
        TimelineTitle = TimelineTitle.Display(
            TestContext.Current?.Metadata.TestName ?? GetType().Name),
    };

    /// <summary>Opens a recording for the current test. The class session is already connected.</summary>
    [Before(HookType.Test, Order = 10)]
    public void AttachTimeline()
    {
        timeline = new AppiumTimeline(TimelineDriver, CreateTimelineOptions());
    }

    /// <summary>Writes frames and the HTML timeline. Does not dispose the driver.</summary>
    /// <returns>A task that completes when artifacts are flushed.</returns>
    [After(HookType.Test, Order = 10)]
    public async Task FlushTimelineAsync()
    {
        if (timeline is not null)
        {
            await timeline.FlushAsync().ConfigureAwait(false);
        }
    }
}
