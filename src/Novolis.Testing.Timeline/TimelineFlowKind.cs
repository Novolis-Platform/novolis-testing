namespace Novolis.Testing.Timeline;

/// <summary>Why a nested timeline section exists in the recording.</summary>
public enum TimelineFlowKind
{
    /// <summary>Ordinary nested work.</summary>
    Default = 0,

    /// <summary>The scenario followed the expected path.</summary>
    Planned,

    /// <summary>The scenario left the expected path.</summary>
    Deviated,

    /// <summary>The scenario was closed or unavailable.</summary>
    Closed,
}
