namespace Novolis.Testing.Timeline;

/// <summary>One image-backed state in an evidence timeline.</summary>
public sealed record TimelineStep(
    int Index,
    string Part,
    string Name,
    string Narration,
    string FrameFile,
    DateTimeOffset CapturedAtUtc,
    int Depth = 0,
    string Flow = "",
    string FlowPath = "",
    string Kind = "",
    IReadOnlyList<string>? Sections = null);
