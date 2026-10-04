namespace Novolis.Testing.Playwright;

/// <summary>One captured step inside a scenario part, optionally nested in a flow.</summary>
public sealed record PlaywrightWalkthroughStep(
    int Index,
    string Part,
    string Name,
    string Narration,
    string FrameFile,
    DateTimeOffset CapturedAtUtc,
    int Depth = 0,
    string Flow = "",
    string FlowPath = "",
    string Kind = "");
