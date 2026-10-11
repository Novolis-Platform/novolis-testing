namespace Novolis.Testing.Timeline;

/// <summary>Structured timeline artifact envelope.</summary>
public sealed record TimelineDocument(
    string Schema,
    DateTimeOffset GeneratedAtUtc,
    TimelineManifest Manifest,
    IReadOnlyList<TimelineEvent> Events,
    IReadOnlyList<object> Telemetry);
