using Novolis.Testing.Timeline;

namespace Novolis.Testing.Unit;

public sealed class TimelineMarkdownRendererTests
{
    [Test]
    public async Task Render_keeps_spaces_in_titles_and_embeds_frames()
    {
        var markdown = TimelineMarkdownRenderer.Render(
            Sample(),
            "../index.md");

        await Assert.That(markdown).Contains("# Game shop from platform");
        await Assert.That(markdown).Contains("- **3. Robin registers the month**");
        await Assert.That(markdown).Contains("1. Set Saturday as a working day");
        await Assert.That(markdown).Contains("![Set Saturday as a working day](frames/08-Set_Saturday_as_a_working_day.png)");
        await Assert.That(markdown).Contains("[All timelines](../index.md)");
        await Assert.That(markdown).DoesNotContain("SetSaturdayasaworkingday");
    }

    internal static TimelineManifest Sample() =>
        new(
            "Game_shop_from_platform",
            null,
            @"d:\artifacts",
            RawCaptureFile: null,
            TraceFile: null,
            FrameHoldMilliseconds: 4000,
            [
                new TimelineStep(
                    8,
                    "3. Robin registers the month",
                    "Set Saturday as a working day",
                    "Game High Street trades Saturday.",
                    "frames/08-Set_Saturday_as_a_working_day.png",
                    DateTimeOffset.UnixEpoch),
            ],
            "timeline.log");
}
