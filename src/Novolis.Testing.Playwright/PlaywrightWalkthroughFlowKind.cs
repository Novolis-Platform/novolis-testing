namespace Novolis.Testing.Playwright;

/// <summary>Why a nested walkthrough flow exists in the recording outline.</summary>
public enum PlaywrightWalkthroughFlowKind
{
    /// <summary>Ordinary nested work. No special marker.</summary>
    Default = 0,

    /// <summary>The day followed the usual clock.</summary>
    Planned,

    /// <summary>The day left the usual clock or left a gap.</summary>
    Deviated,

    /// <summary>The shop was shut. Not a missed record.</summary>
    Closed,
}
