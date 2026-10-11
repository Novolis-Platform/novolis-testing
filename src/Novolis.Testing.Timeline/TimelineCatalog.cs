using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Novolis.Testing.Timeline;

/// <summary>Indexes portable timeline recordings beneath one artifact root.</summary>
public static class TimelineCatalog
{
    private static readonly object Gate = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Index Markdown file name under a collection root.</summary>
    public const string MarkdownFileName = "index.md";

    /// <summary>Index HTML file name under a collection root.</summary>
    public const string HtmlFileName = "index.html";

    /// <summary>
    /// Re-renders every completed <c>timeline.json</c> beneath <paramref name="root"/>,
    /// writes a collection index, and optionally removes older recordings for the same
    /// class and test identity.
    /// </summary>
    public static IReadOnlyList<TimelineCatalogEntry> Refresh(
        string root,
        bool pruneOlderRecordings = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        lock (Gate)
        {
            return RefreshCore(Path.GetFullPath(root), pruneOlderRecordings);
        }
    }

    /// <summary>Runs an operation while catalog writes are serialized.</summary>
    public static void Synchronize(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        lock (Gate)
        {
            action();
        }
    }

    private static IReadOnlyList<TimelineCatalogEntry> RefreshCore(
        string root,
        bool pruneOlderRecordings)
    {
        Directory.CreateDirectory(root);
        var latest = new Dictionary<string, TimelineCatalogEntry>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var jsonPath in Directory.EnumerateFiles(
                     root,
                     "timeline.json",
                     SearchOption.AllDirectories))
        {
            var directory = Path.GetDirectoryName(jsonPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            TimelineDocument? document;
            try
            {
                document = JsonSerializer.Deserialize<TimelineDocument>(
                    ReadShared(jsonPath),
                    JsonOptions);
            }
            catch (JsonException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }

            if (document is null)
            {
                continue;
            }

            var relative = Path.GetRelativePath(root, directory).Replace('\\', '/');
            var markdownHref = Path.GetRelativePath(
                    directory,
                    Path.Combine(root, MarkdownFileName))
                .Replace('\\', '/');
            var htmlHref = Path.GetRelativePath(
                    directory,
                    Path.Combine(root, HtmlFileName))
                .Replace('\\', '/');
            var logs = ReadLogs(directory, document.Manifest.LogFileName);
            File.WriteAllText(
                Path.Combine(directory, "timeline.md"),
                TimelineMarkdownRenderer.Render(document.Manifest, markdownHref));
            File.WriteAllText(
                Path.Combine(directory, "timeline.html"),
                TimelineHtmlRenderer.Render(
                    document.Manifest,
                    document.Events,
                    document.Telemetry,
                    logs,
                    htmlHref,
                    asset => TimelineAssets.ToDataUriOrPath(directory, asset)));

            var className = ClassNameOf(relative);
            var testName = TestNameOf(relative);
            var key = className + "/" + testName;
            var recorded = document.Manifest.Steps.Count == 0
                ? document.GeneratedAtUtc
                : document.Manifest.Steps.Max(step => step.CapturedAtUtc);
            var entry = new TimelineCatalogEntry(
                className,
                testName,
                TimelineTitle.Display(document.Manifest.Title),
                TimelineOutline.SectionCount(document.Manifest.Steps),
                document.Manifest.Steps.Count,
                relative,
                recorded,
                document);
            if (!latest.TryGetValue(key, out var existing)
                || entry.RecordedAtUtc >= existing.RecordedAtUtc)
            {
                latest[key] = entry;
            }
        }

        var entries = latest.Values
            .OrderBy(entry => entry.ClassName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (pruneOlderRecordings)
        {
            PruneOlderRecordings(
                root,
                entries.Select(entry =>
                    Path.GetFullPath(Path.Combine(root, entry.RelativeDirectory)))
                    .ToArray());
        }

        File.WriteAllText(Path.Combine(root, MarkdownFileName), RenderMarkdown(entries));
        File.WriteAllText(Path.Combine(root, HtmlFileName), RenderHtml(entries));
        return entries;
    }

    /// <summary>Removes completed recordings that are not in <paramref name="keep"/>.</summary>
    public static void PruneOlderRecordings(
        string root,
        IReadOnlyCollection<string> keep)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentNullException.ThrowIfNull(keep);
        if (!Directory.Exists(root))
        {
            return;
        }

        var retained = new HashSet<string>(
            keep.Select(Path.GetFullPath),
            StringComparer.OrdinalIgnoreCase);
        foreach (var jsonPath in Directory.EnumerateFiles(
                     root,
                     "timeline.json",
                     SearchOption.AllDirectories))
        {
            var directory = Path.GetDirectoryName(jsonPath);
            if (string.IsNullOrWhiteSpace(directory)
                || retained.Contains(Path.GetFullPath(directory)))
            {
                continue;
            }

            TryDeleteDirectory(directory);
        }
    }

    /// <summary>Renders the collection as Markdown.</summary>
    public static string RenderMarkdown(IReadOnlyList<TimelineCatalogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var text = new StringBuilder();
        text.AppendLine("# Timelines");
        text.AppendLine();
        text.AppendLine(
            "Latest recording of each scenario. timeline.md is the readable story and timeline.html is the player.");
        text.AppendLine();
        if (entries.Count == 0)
        {
            text.AppendLine("No timelines have been written yet.");
            text.AppendLine();
            return text.ToString();
        }

        string? lastClass = null;
        foreach (var entry in entries)
        {
            if (!string.Equals(entry.ClassName, lastClass, StringComparison.Ordinal))
            {
                text.AppendLine($"## {TimelineTitle.FromTypeName(entry.ClassName)}");
                text.AppendLine();
                lastClass = entry.ClassName;
            }

            text.Append("- [")
                .Append(entry.Title)
                .Append("](")
                .Append(entry.MarkdownHref)
                .Append(") · [Play](")
                .Append(entry.HtmlHref)
                .Append(") · ")
                .Append(entry.PartCount.ToString(CultureInfo.InvariantCulture))
                .Append(" sections · ")
                .Append(entry.StepCount.ToString(CultureInfo.InvariantCulture))
                .AppendLine(" frames");
        }

        text.AppendLine();
        return text.ToString();
    }

    /// <summary>Renders the collection as a static HTML index.</summary>
    public static string RenderHtml(IReadOnlyList<TimelineCatalogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var navigation = new StringBuilder();
        if (entries.Count == 0)
        {
            navigation.Append("<p class=\"muted\">No timelines have been written yet.</p>");
        }
        else
        {
            string? lastClass = null;
            foreach (var entry in entries)
            {
                if (!string.Equals(entry.ClassName, lastClass, StringComparison.Ordinal))
                {
                    if (lastClass is not null)
                    {
                        navigation.Append("</ul></section>");
                    }

                    navigation.Append("<section><h2>")
                        .Append(Encode(TimelineTitle.FromTypeName(entry.ClassName)))
                        .Append("</h2><ul>");
                    lastClass = entry.ClassName;
                }

                navigation.Append("<li><a href=\"")
                    .Append(Encode(entry.HtmlHref))
                    .Append("\">")
                    .Append(Encode(entry.Title))
                    .Append("</a><span>")
                    .Append(entry.PartCount.ToString(CultureInfo.InvariantCulture))
                    .Append(" sections · ")
                    .Append(entry.StepCount.ToString(CultureInfo.InvariantCulture))
                    .Append(" frames</span><a class=\"story\" href=\"")
                    .Append(Encode(entry.MarkdownHref))
                    .Append("\">Markdown</a></li>");
            }

            if (lastClass is not null)
            {
                navigation.Append("</ul></section>");
            }
        }

        const string template = """
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8"/>
              <meta name="viewport" content="width=device-width, initial-scale=1"/>
              <title>Timelines</title>
              <style>
                :root { --bg:#07131c; --surface:#0d202b; --raised:#12303d; --border:#28566a; --text:#eef8fb; --muted:#a8c3cc; --accent:#6fe4df; }
                * { box-sizing:border-box; }
                html, body { min-height:100%; margin:0; }
                body { color:var(--text); background:var(--bg); font:16px/1.45 "Segoe UI",sans-serif; }
                main { max-width:62rem; margin:0 auto; padding:2rem 1.25rem 3rem; }
                h1 { margin:0 0 .35rem; }
                h2 { color:var(--accent); font-size:1rem; margin:1.4rem 0 .45rem; }
                .lede, .muted, li span { color:var(--muted); }
                section { background:var(--surface); border:1px solid var(--border); padding:.8rem 1rem; margin-top:.8rem; }
                ul { list-style:none; padding:0; margin:0; }
                li { display:grid; grid-template-columns:minmax(0,1fr) auto auto; gap:.8rem; align-items:baseline; padding:.6rem 0; border-top:1px solid var(--border); }
                li:first-child { border-top:0; }
                a { color:var(--text); }
                a:hover { color:var(--accent); }
                .story { color:var(--muted); font-size:.85rem; }
                @media (max-width:720px) { li { grid-template-columns:1fr; gap:.15rem; } }
              </style>
            </head>
            <body>
              <main>
                <h1>Timelines</h1>
                <p class="lede">Latest image-backed evidence recordings, grouped by test class.</p>
                __NAV__
              </main>
            </body>
            </html>
            """;
        return template.Replace("__NAV__", navigation.ToString(), StringComparison.Ordinal);
    }

    private static string[] ReadLogs(string directory, string logFileName)
    {
        var path = Path.Combine(directory, "logs", logFileName);
        if (!File.Exists(path))
        {
            return [];
        }

        try
        {
            return File.ReadAllLines(path);
        }
        catch (IOException)
        {
            return [];
        }
    }

    private static string ReadShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string ClassNameOf(string relative)
    {
        var slash = relative.IndexOf('/', StringComparison.Ordinal);
        return slash < 0 ? relative : relative[..slash];
    }

    private static string TestNameOf(string relative)
    {
        var parts = relative.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? parts[1] : parts[0];
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
