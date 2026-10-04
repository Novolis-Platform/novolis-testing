namespace Novolis.Testing.Playwright;

/// <summary>Launch and recording options for one Playwright browser session.</summary>
public sealed record PlaywrightSessionOptions
{
    /// <summary>Absolute URL used as Playwright <c>BaseURL</c>.</summary>
    public Uri? BaseUrl { get; init; }

    /// <summary>Directory that receives video, frames, and the walkthrough manifest.</summary>
    public string? ArtifactDirectory { get; init; }

    /// <summary>When <see langword="true"/>, Chromium runs without a window. Default is headless.</summary>
    public bool Headless { get; init; } = true;

    /// <summary>When <see langword="true"/>, Playwright writes a WebM video. Default is on.</summary>
    public bool RecordVideo { get; init; } = true;

    /// <summary>When <see langword="true"/>, Playwright writes a trace zip next to the video.</summary>
    public bool RecordTrace { get; init; } = true;

    /// <summary>Context viewport width in CSS pixels.</summary>
    public int ViewportWidth { get; init; } = 1440;

    /// <summary>Context viewport height in CSS pixels.</summary>
    public int ViewportHeight { get; init; } = 900;

    /// <summary>Walkthrough title stored in the manifest.</summary>
    public string WalkthroughTitle { get; init; } = "Playwright walkthrough";

    /// <summary>
    /// Resolves headed vs headless. <c>NOVOLIS_PLAYWRIGHT_HEADED=1</c> forces a visible browser.
    /// </summary>
    /// <returns>Effective headless flag.</returns>
    public bool ResolveHeadless()
    {
        var headed = Environment.GetEnvironmentVariable("NOVOLIS_PLAYWRIGHT_HEADED");
        return headed is "1" or "true" or "TRUE" ? false : Headless;
    }
}
