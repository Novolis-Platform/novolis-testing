namespace Novolis.Testing.Playwright;

/// <summary>One captured step in a recorded walkthrough.</summary>
public sealed record PlaywrightWalkthroughStep(
    int Index,
    string Name,
    string FrameFile,
    DateTimeOffset CapturedAtUtc);
