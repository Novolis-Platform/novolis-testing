namespace Novolis.Testing.Timeline;

/// <summary>Portable metadata for one evidence timeline recording.</summary>
public sealed record TimelineManifest(
    string Title,
    string? BaseUrl,
    string ArtifactDirectory,
    string? RawCaptureFile,
    string? TraceFile,
    int FrameHoldMilliseconds,
    IReadOnlyList<TimelineStep> Steps,
    string LogFileName);
