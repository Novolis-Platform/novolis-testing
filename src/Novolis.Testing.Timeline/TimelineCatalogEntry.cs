namespace Novolis.Testing.Timeline;

/// <summary>One latest recording in a timeline collection.</summary>
public sealed record TimelineCatalogEntry(
    string ClassName,
    string TestName,
    string Title,
    int PartCount,
    int StepCount,
    string RelativeDirectory,
    DateTimeOffset RecordedAtUtc,
    TimelineDocument Document)
{
    /// <summary>Manifest represented by this catalog entry.</summary>
    public TimelineManifest Manifest => Document.Manifest;

    /// <summary>Relative path from the collection root to the JSON document.</summary>
    public string JsonHref => RelativeDirectory.Replace('\\', '/') + "/timeline.json";

    /// <summary>Relative path from the collection root to the portable HTML document.</summary>
    public string HtmlHref => RelativeDirectory.Replace('\\', '/') + "/timeline.html";

    /// <summary>Relative path from the collection root to the Markdown story.</summary>
    public string MarkdownHref => RelativeDirectory.Replace('\\', '/') + "/timeline.md";
}
