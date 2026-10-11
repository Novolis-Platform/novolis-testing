using Novolis.Testing.Timeline;

namespace Novolis.Testing.Appium;

/// <summary>Artifact options for one per-test Appium evidence timeline.</summary>
public sealed record AppiumTimelineOptions
{
    /// <summary>Directory that receives frames and the timeline recording.</summary>
    public string? ArtifactDirectory { get; init; }

    /// <summary>How long the HTML timeline holds each frame, in milliseconds.</summary>
    public int FrameHoldMilliseconds { get; init; } = TimelineRecorder.DefaultFrameHoldMilliseconds;

    /// <summary>Timeline title stored in the manifest.</summary>
    public string TimelineTitle { get; init; } = "Appium timeline";
}
