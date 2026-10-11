namespace Novolis.Testing.Timeline;

/// <summary>Optional metadata and artifact settings for one timeline recording.</summary>
public sealed record TimelineRecorderOptions
{
    /// <summary>Optional URL associated with the recording.</summary>
    public string? BaseUrl { get; init; }

    /// <summary>How long the HTML player holds each frame.</summary>
    public int FrameHoldMilliseconds { get; init; } = TimelineRecorder.DefaultFrameHoldMilliseconds;

    /// <summary>Optional relative raw capture file, such as a WebM.</summary>
    public string? RawCaptureFile { get; init; }

    /// <summary>Optional relative trace file.</summary>
    public string? TraceFile { get; init; }

    /// <summary>File name written beneath the recording's logs directory.</summary>
    public string LogFileName { get; init; } = "timeline.log";
}
