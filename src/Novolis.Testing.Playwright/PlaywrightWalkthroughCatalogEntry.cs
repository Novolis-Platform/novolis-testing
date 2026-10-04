namespace Novolis.Testing.Playwright;

/// <summary>One latest recording in a walkthrough collection.</summary>
public sealed record PlaywrightWalkthroughCatalogEntry(
    string ClassName,
    string TestName,
    string Title,
    int PartCount,
    int StepCount,
    string RelativeDirectory,
    DateTimeOffset RecordedAtUtc,
    PlaywrightWalkthroughManifest Manifest)
{
    /// <summary>Relative path from the catalog root to this recording's portable HTML.</summary>
    public string HtmlHref => RelativeDirectory.Replace('\\', '/') + "/walkthrough.html";
}
