using Novolis.Testing.Playwright;

namespace Novolis.Testing.Unit;

public sealed class PlaywrightWalkthroughMarkdownTests
{
    [Test]
    public async Task Render_keeps_spaces_in_titles_and_embeds_frames()
    {
        var markdown = PlaywrightWalkthroughMarkdown.Render(
            Sample(),
            "../index.md");

        await Assert.That(markdown).Contains("# Game shop from platform");
        await Assert.That(markdown).Contains("- **3. Robin registers the month**");
        await Assert.That(markdown).Contains("1. Set Saturday as a working day");
        await Assert.That(markdown).Contains("![Set Saturday as a working day](frames/08-Set_Saturday_as_a_working_day.png)");
        await Assert.That(markdown).Contains("[All walkthroughs](../index.md)");
        await Assert.That(markdown).DoesNotContain("SetSaturdayasaworkingday");
    }

    internal static PlaywrightWalkthroughManifest Sample() =>
        new(
            "Game_shop_from_platform",
            null,
            @"d:\artifacts",
            RawCaptureFile: null,
            TraceFile: null,
            FrameHoldMilliseconds: 4000,
            [
                new PlaywrightWalkthroughStep(
                    8,
                    "3. Robin registers the month",
                    "Set Saturday as a working day",
                    "Game High Street trades Saturday.",
                    "frames/08-Set_Saturday_as_a_working_day.png",
                    DateTimeOffset.UnixEpoch),
            ]);
}
