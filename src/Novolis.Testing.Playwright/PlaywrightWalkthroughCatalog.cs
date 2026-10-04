using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Novolis.Testing.Playwright;

/// <summary>Rewrites recordings and writes a collection index that links every latest walkthrough.</summary>
public static class PlaywrightWalkthroughCatalog
{
    private static readonly object Gate = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>Index Markdown file name under the artifact root.</summary>
    public const string MarkdownFileName = "index.md";

    /// <summary>Index HTML file name under the artifact root.</summary>
    public const string HtmlFileName = "index.html";

    /// <summary>
    /// Re-renders every <c>walkthrough.json</c> under <paramref name="root"/> and writes
    /// <see cref="MarkdownFileName"/> plus <see cref="HtmlFileName"/>.
    /// </summary>
    /// <param name="root">Playwright artifact root.</param>
    /// <returns>Latest recording per test, newest first within each class.</returns>
    public static IReadOnlyList<PlaywrightWalkthroughCatalogEntry> Refresh(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        lock (Gate)
        {
            return RefreshCore(root);
        }
    }

    /// <summary>Runs <paramref name="action"/> while no other catalog write is in flight.</summary>
    public static void Synchronize(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        lock (Gate)
        {
            action();
        }
    }

    /// <summary>Stable in-page id for a class and test name.</summary>
    public static string AnchorOf(string className, string testName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(className);
        ArgumentException.ThrowIfNullOrWhiteSpace(testName);
        var raw = className + "-" + testName;
        var text = new StringBuilder(raw.Length);
        foreach (var c in raw)
        {
            text.Append(char.IsAsciiLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-');
        }

        return text.ToString().Trim('-');
    }

    private static IReadOnlyList<PlaywrightWalkthroughCatalogEntry> RefreshCore(string root)
    {
        Directory.CreateDirectory(root);
        var latest = new Dictionary<string, PlaywrightWalkthroughCatalogEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var jsonPath in Directory.EnumerateFiles(root, "walkthrough.json", SearchOption.AllDirectories))
        {
            var directory = Path.GetDirectoryName(jsonPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            PlaywrightWalkthroughManifest? manifest;
            try
            {
                manifest = JsonSerializer.Deserialize<PlaywrightWalkthroughManifest>(
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

            if (manifest is null)
            {
                continue;
            }

            var relative = Path.GetRelativePath(root, directory).Replace('\\', '/');
            var indexHref = Path.GetRelativePath(directory, Path.Combine(root, MarkdownFileName))
                .Replace('\\', '/');
            var htmlIndexHref = Path.GetRelativePath(directory, Path.Combine(root, HtmlFileName))
                .Replace('\\', '/');
            File.WriteAllText(
                Path.Combine(directory, "walkthrough.md"),
                PlaywrightWalkthroughMarkdown.Render(manifest, indexHref));
            File.WriteAllText(
                Path.Combine(directory, "walkthrough.html"),
                PlaywrightWalkthroughPlayer.Render(manifest, htmlIndexHref));

            var className = ClassNameOf(relative);
            var testName = TestNameOf(relative);
            var key = className + "/" + testName;
            var recorded = manifest.Steps.Count == 0
                ? DateTimeOffset.UnixEpoch
                : manifest.Steps.Max(step => step.CapturedAtUtc);
            var entry = new PlaywrightWalkthroughCatalogEntry(
                className,
                testName,
                PlaywrightWalkthroughTitle.Display(manifest.Title),
                PlaywrightWalkthroughOutline.SectionCount(manifest.Steps),
                manifest.Steps.Count,
                relative,
                recorded,
                manifest);
            if (!latest.TryGetValue(key, out var existing) || entry.RecordedAtUtc >= existing.RecordedAtUtc)
            {
                latest[key] = entry;
            }
        }

        var entries = latest.Values
            .OrderBy(entry => entry.ClassName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        File.WriteAllText(Path.Combine(root, MarkdownFileName), RenderMarkdown(entries));
        File.WriteAllText(Path.Combine(root, HtmlFileName), RenderHtml(entries));
        return entries;
    }

    /// <summary>Collection index as Markdown.</summary>
    /// <param name="entries">Latest recording per test.</param>
    /// <returns>Markdown text.</returns>
    public static string RenderMarkdown(IReadOnlyList<PlaywrightWalkthroughCatalogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var text = new StringBuilder();
        text.AppendLine("# Walkthroughs");
        text.AppendLine();
        text.AppendLine("Latest recording of each scenario. Markdown is the story. HTML is the same pages with Play / Next.");
        text.AppendLine();
        if (entries.Count == 0)
        {
            text.AppendLine("No walkthroughs have been written yet.");
            text.AppendLine();
            return text.ToString();
        }

        string? lastClass = null;
        foreach (var entry in entries)
        {
            if (!string.Equals(entry.ClassName, lastClass, StringComparison.Ordinal))
            {
                text.AppendLine($"## {PlaywrightWalkthroughTitle.FromTypeName(entry.ClassName)}");
                text.AppendLine();
                lastClass = entry.ClassName;
            }

            var md = $"{entry.RelativeDirectory}/walkthrough.md";
            var html = $"{entry.RelativeDirectory}/walkthrough.html";
            text.AppendLine(
                $"- [{entry.Title}]({md}) · [Play]({html}) · {entry.PartCount} sections · {entry.StepCount} steps");
        }

        text.AppendLine();
        return text.ToString();
    }

    /// <summary>Collection index as a static HTML document.</summary>
    /// <param name="entries">Latest recording per test.</param>
    /// <returns>HTML document.</returns>
    public static string RenderHtml(IReadOnlyList<PlaywrightWalkthroughCatalogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var body = new StringBuilder();
        body.AppendLine("<h1>Walkthroughs</h1>");
        body.AppendLine("<p>Click a scenario. It stays on this page. Frames load from the same folder as this file.</p>");
        if (entries.Count == 0)
        {
            body.AppendLine("<p>No walkthroughs have been written yet.</p>");
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
                        body.AppendLine("</ul>");
                    }

                    body.Append("<h2>")
                        .Append(Encode(PlaywrightWalkthroughTitle.FromTypeName(entry.ClassName)))
                        .AppendLine("</h2>");
                    body.AppendLine("<ul>");
                    lastClass = entry.ClassName;
                }

                body.Append("<li><a href=\"#")
                    .Append(Encode(entry.Anchor))
                    .Append("\">")
                    .Append(Encode(entry.Title))
                    .Append("</a> · ")
                    .Append(entry.PartCount.ToString(CultureInfo.InvariantCulture))
                    .Append(" sections · ")
                    .Append(entry.StepCount.ToString(CultureInfo.InvariantCulture))
                    .AppendLine(" steps</li>");
            }

            if (lastClass is not null)
            {
                body.AppendLine("</ul>");
            }

            foreach (var entry in entries)
            {
                body.Append("<section class=\"scenario\" id=\"")
                    .Append(Encode(entry.Anchor))
                    .AppendLine("\">");
                body.Append("<h2>")
                    .Append(Encode(entry.Title))
                    .AppendLine("</h2>");
                body.Append("<p class=\"meta\">")
                    .Append(entry.PartCount.ToString(CultureInfo.InvariantCulture))
                    .Append(" sections · ")
                    .Append(entry.StepCount.ToString(CultureInfo.InvariantCulture))
                    .AppendLine(" steps</p>");
                body.Append(PlaywrightWalkthroughPlayer.RenderArticles(entry.Manifest, entry.RelativeDirectory));
                body.AppendLine("</section>");
            }
        }

        const string template = """
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8"/>
              <meta name="viewport" content="width=device-width, initial-scale=1"/>
              <title>Walkthroughs</title>
              <style>
                body { font-family: Georgia, "Times New Roman", serif; margin: 0 auto; max-width: 52rem; padding: 2rem 1.25rem 4rem; line-height: 1.5; color: #1a1a1a; background: #f7f4ee; }
                h1, h2 { font-family: Segoe UI, sans-serif; font-weight: 650; line-height: 1.3; }
                h1 { font-size: 1.8rem; }
                h2 { font-size: 1.2rem; margin-top: 1.75rem; }
                a { color: #1d4b6e; }
                ul { padding-left: 1.25rem; }
                li { margin: .45rem 0; overflow-wrap: break-word; }
                section.scenario { margin-top: 3rem; padding-top: 1rem; border-top: 1px solid #d4cbb8; scroll-margin-top: 1rem; }
                section.scenario img { display: block; width: 100%; height: auto; background: #fff; border: 1px solid #d4cbb8; margin: .75rem 0 1.5rem; }
                .meta { color: #4a4a4a; }
              </style>
            </head>
            <body>
            __BODY__
            </body>
            </html>
            """;
        return template.Replace("__BODY__", body.ToString(), StringComparison.Ordinal);
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

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
