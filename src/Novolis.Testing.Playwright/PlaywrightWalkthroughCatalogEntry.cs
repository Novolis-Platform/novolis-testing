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
    /// <summary>In-page anchor for the overview document.</summary>
    public string Anchor => PlaywrightWalkthroughCatalog.AnchorOf(ClassName, TestName);
}
