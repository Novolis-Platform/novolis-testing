namespace Novolis.Testing.Playwright;

/// <summary>JSON manifest written beside video and screenshot frames.</summary>
public sealed record PlaywrightWalkthroughManifest(
    string Title,
    string? BaseUrl,
    string ArtifactDirectory,
    string? VideoFile,
    string? TraceFile,
    IReadOnlyList<PlaywrightWalkthroughStep> Steps);
