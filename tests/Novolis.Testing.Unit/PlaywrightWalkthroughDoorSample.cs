using Novolis.Testing.Playwright;

namespace Novolis.Testing.Unit;

/// <summary>
/// In-code door scenario used by player tests.
/// Built so <c>DumpVar</c> can emit a C# initializer from a live instance.
/// </summary>
internal static class PlaywrightWalkthroughDoorSample
{
    /// <summary>Scenario, section, nested section, two steps.</summary>
    public static PlaywrightWalkthroughManifest Create() =>
        new(
            "Hours door",
            "http://localhost:1/",
            @"d:\artifacts",
            RawCaptureFile: null,
            TraceFile: null,
            FrameHoldMilliseconds: 4000,
            [
                new PlaywrightWalkthroughStep(
                    1,
                    "1. Sign in",
                    "Open door",
                    "The week is behind the door.",
                    "frames/01-Open_door.png",
                    DateTimeOffset.Parse("2026-10-04T23:12:34.567Z"),
                    Depth: 1,
                    Flow: "1. Sign in",
                    FlowPath: "1. Sign in",
                    Kind: "",
                    Sections: ["1. Sign in"]),
                new PlaywrightWalkthroughStep(
                    2,
                    "1. Sign in",
                    "Open My hours",
                    "The usual clock is a form.",
                    "frames/02-Open_My_hours.png",
                    DateTimeOffset.Parse("2026-10-04T23:12:38.567Z"),
                    Depth: 2,
                    Flow: "Set usual hours",
                    FlowPath: "1. Sign in · Set usual hours",
                    Kind: "",
                    Sections: ["1. Sign in", "Set usual hours"]),
            ]);
}
