namespace Novolis.Testing.Timeline;

/// <summary>One timestamped observation in an evidence timeline.</summary>
public sealed record TimelineEvent(
    int Sequence,
    DateTimeOffset CapturedAtUtc,
    string Name,
    string Status,
    string? Detail,
    IReadOnlyDictionary<string, object?>? Metadata);
