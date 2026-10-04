namespace Novolis.Testing.Playwright;

/// <summary>JSON manifest written beside screenshot frames and the HTML recording.</summary>
public sealed record PlaywrightWalkthroughManifest(
    string Title,
    string? BaseUrl,
    string ArtifactDirectory,
    string? RawCaptureFile,
    string? TraceFile,
    int FrameHoldMilliseconds,
    IReadOnlyList<PlaywrightWalkthroughStep> Steps);
