namespace Novolis.Testing.Timeline;

/// <summary>One open, close, or frame operation in a nested timeline outline.</summary>
public sealed record TimelineOutlineOp(
    string Action,
    int Depth,
    string Heading,
    string Kind,
    TimelineStep? Step);
