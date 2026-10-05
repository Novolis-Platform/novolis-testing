using Novolis.CodeGen.Reflection.Dump;
using Novolis.Testing.Playwright;

namespace Novolis.Testing.Unit;

public sealed class PlaywrightWalkthroughPlayerTests
{
    [Test]
    public async Task Dump_writes_the_door_sample_as_csharp()
    {
        var source = PlaywrightWalkthroughDoorSample.Create().DumpVar();
        await Assert.That(source).Contains("Hours door");
        await Assert.That(source).Contains("Open door");
        await Assert.That(source).Contains("frames/01-Open_door.png");
        await Assert.That(source).DoesNotContain("base64");
    }

    [Test]
    public async Task Render_groups_parts_and_shows_narration_not_a_realtime_video()
    {
        var html = PlaywrightWalkthroughPlayer.Render(PlaywrightWalkthroughDoorSample.Create());

        await Assert.That(html).Contains("1 sections · 2 steps");
        await Assert.That(html).Contains("The week is behind the door.");
        await Assert.That(html).Contains("Hours door");
        await Assert.That(html).Contains("Play scenario");
        await Assert.That(html).Contains("<a href=\"#step-1\">Open door</a>");
        await Assert.That(html).Contains("<details class=\"section\" open><summary>1. Sign in</summary>");
        await Assert.That(html).Contains("<details class=\"section\" open><summary>Set usual hours</summary>");
        await Assert.That(html).Contains("class=\"step is-current\" id=\"step-1\"");
        await Assert.That(html).Contains("class=\"viewport\"");
        await Assert.That(html).Contains("href=\"walkthrough.md\"");
        await Assert.That(html).Contains("src=\"frames/01-Open_door.png\"");
        await Assert.That(html).DoesNotContain("text-transform: uppercase");
        await Assert.That(html).DoesNotContain("<video autoplay");
    }

    [Test]
    public async Task Render_compiles_frames_through_resolveAsset()
    {
        var html = PlaywrightWalkthroughPlayer.Render(
            PlaywrightWalkthroughDoorSample.Create(),
            indexHref: "../index.html",
            resolveAsset: path => "data:image/png;base64,abc" + path.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));

        await Assert.That(html).Contains("data:image/png;base64,");
        await Assert.That(html).Contains("All walkthroughs");
        await Assert.That(html).Contains("href=\"walkthrough.md\"");
        await Assert.That(html).DoesNotContain("src=\"frames/01-Open_door.png\"");
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
        await Assert.That(html).DoesNotContain("trace.zip");
    }

    [Test]
    public async Task Render_puts_kind_on_the_section_not_every_step()
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
                    Depth: 2,
                    Flow: "Set usual hours",
                    FlowPath: "Usual hours · Set usual hours",
                    Kind: "",
                    Sections: ["Usual hours", "Set usual hours"]),
                new PlaywrightWalkthroughStep(
                    2,
                    "Exceptions",
                    "Type 10:00–18:00",
                    "Late Thursday. Same length, different clock.",
                    "frames/02.png",
                    DateTimeOffset.UnixEpoch,
                    Depth: 3,
                    Flow: "I worked different hours",
                    FlowPath: "Exceptions · Thursday 1 October · Changed · I worked different hours",
                    Kind: "deviated",
                    Sections: ["Exceptions", "Thursday 1 October · Changed", "I worked different hours"]),
            ]));

        await Assert.That(html).Contains("kind-badge deviated\">Deviated</span>");
        await Assert.That(html).Contains("Thursday 1 October");
        await Assert.That(html).Contains("<summary>I worked different hours");
        await Assert.That(html).Contains("<summary>Set usual hours</summary>");
        await Assert.That(html).DoesNotContain("This flow departed from the usual clock.");
    }
}
