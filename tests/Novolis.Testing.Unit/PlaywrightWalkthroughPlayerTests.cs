using Novolis.Testing.Playwright;

namespace Novolis.Testing.Unit;

public sealed class PlaywrightWalkthroughPlayerTests
{
    [Test]
    public async Task Render_groups_parts_and_shows_narration_not_a_realtime_video()
    {
        var html = PlaywrightWalkthroughPlayer.Render(new PlaywrightWalkthroughManifest(
            "Game shop <month>",
            "http://localhost:1/",
            @"d:\artifacts",
            RawCaptureFile: null,
            TraceFile: null,
            FrameHoldMilliseconds: 4000,
            [
                new PlaywrightWalkthroughStep(1, "Platform", "Sign in", "The platform system opens Hours.", "frames/01.png", DateTimeOffset.UnixEpoch),
                new PlaywrightWalkthroughStep(2, "Shop roster", "Add clerk", "The customer adds a clerk.", "frames/02.png", DateTimeOffset.UnixEpoch),
            ]));

        await Assert.That(html).Contains("Scenario recording. Starts paused.");
        await Assert.That(html).Contains("2 parts · 2 steps");
        await Assert.That(html).Contains("The platform system opens Hours.");
        await Assert.That(html).Contains("Game shop &lt;month&gt;");
        await Assert.That(html).Contains("Play scenario");
        await Assert.That(html).DoesNotContain("<video autoplay");
    }

    [Test]
    public async Task Render_hides_realtime_webm_under_raw_capture()
    {
        var html = PlaywrightWalkthroughPlayer.Render(new PlaywrightWalkthroughManifest(
            "Ada",
            null,
            @"d:\artifacts",
            "Ada.webm",
            "trace.zip",
            1800,
            [new PlaywrightWalkthroughStep(1, "Flex", "Paint", "Ada paints customer time.", "frames/01.png", DateTimeOffset.UnixEpoch)]));

        await Assert.That(html).Contains("Raw capture (real-time, usually too fast to read)");
        await Assert.That(html).Contains("src=\"Ada.webm\"");
        await Assert.That(html).DoesNotContain("<video autoplay");
    }

    [Test]
    public async Task Render_nests_deviated_flow_under_its_parent()
    {
        var html = PlaywrightWalkthroughPlayer.Render(new PlaywrightWalkthroughManifest(
            "Jamie week",
            null,
            @"d:\artifacts",
            RawCaptureFile: null,
            TraceFile: null,
            FrameHoldMilliseconds: 4000,
            [
                new PlaywrightWalkthroughStep(
                    1,
                    "Usual hours",
                    "Open My hours",
                    "The usual clock is a form, not a strip.",
                    "frames/01.png",
                    DateTimeOffset.UnixEpoch,
                    Depth: 1,
                    Flow: "Set usual hours",
                    FlowPath: "Set usual hours"),
                new PlaywrightWalkthroughStep(
                    2,
                    "Exceptions",
                    "Type 10:00–18:00",
                    "Late Thursday. Same length, different clock.",
                    "frames/02.png",
                    DateTimeOffset.UnixEpoch,
                    Depth: 2,
                    Flow: "I worked different hours",
                    FlowPath: "Thursday 1 October · Changed · I worked different hours",
                    Kind: "deviated"),
            ]));

        await Assert.That(html).Contains("This flow departed from the usual clock.");
        await Assert.That(html).Contains("\"kind\":\"deviated\"");
        await Assert.That(html).Contains("\"depth\":2");
        await Assert.That(html).Contains("Thursday 1 October");
        await Assert.That(html).Contains("I worked different hours");
    }
}
