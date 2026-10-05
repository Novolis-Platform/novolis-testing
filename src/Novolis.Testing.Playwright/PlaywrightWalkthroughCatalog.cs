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
                PlaywrightWalkthroughPlayer.Render(
                    manifest,
                    htmlIndexHref,
                    relative => PlaywrightWalkthroughAssets.ToDataUriOrPath(directory, relative)));

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
        PlaywrightArtifactStore.PruneOlderRecordings(
            root,
            entries.Select(entry => Path.GetFullPath(Path.Combine(root, entry.RelativeDirectory))).ToArray());
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
        text.AppendLine("Latest recording of each scenario. walkthrough.md is the LLM-readable story. walkthrough.html is the player.");
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

            text.AppendLine(
                $"- [{entry.Title}]({entry.MarkdownHref}) · [Play]({entry.HtmlHref}) · {entry.PartCount} sections · {entry.StepCount} steps");
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
        var nav = new StringBuilder();
        if (entries.Count == 0)
        {
            nav.AppendLine("<p class=\"meta\">No walkthroughs have been written yet.</p>");
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
                        nav.AppendLine("</ul></li>");
                    }

                    nav.Append("<li class=\"suite\"><h2>")
                        .Append(Encode(PlaywrightWalkthroughTitle.FromTypeName(entry.ClassName)))
                        .AppendLine("</h2><ul>");
                    lastClass = entry.ClassName;
                }

                nav.Append("<li><a class=\"scenario\" href=\"")
                    .Append(Encode(entry.HtmlHref))
                    .Append("\">")
                    .Append("<span class=\"title\">")
                    .Append(Encode(entry.Title))
                    .Append("</span><span class=\"meta\">")
                    .Append(entry.PartCount.ToString(CultureInfo.InvariantCulture))
                    .Append(" sections · ")
                    .Append(entry.StepCount.ToString(CultureInfo.InvariantCulture))
                    .Append(" steps</span></a><a class=\"markdown\" href=\"")
                    .Append(Encode(entry.MarkdownHref))
                    .AppendLine("\">Markdown</a></li>");
            }

            if (lastClass is not null)
            {
                nav.AppendLine("</ul></li>");
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
                :root {
                  --bg: #010D18;
                  --surface: #051730;
                  --raised: #072041;
                  --border: #093D6F;
                  --text: #E6FBFF;
                  --muted: #2AA5FF;
                  --accent: #2FDFFF;
                  --fill: #237CFF;
                  --on-fill: #EFFDFF;
                }
                * { box-sizing: border-box; }
                html, body { height: 100%; margin: 0; }
                body { font-family: "Segoe UI", sans-serif; color: var(--text); background: var(--bg); }
                .layout { display: grid; grid-template-columns: minmax(18rem, 26rem) 1fr; height: 100vh; }
                @media (max-width: 900px) { .layout { grid-template-columns: 1fr; } }
                aside { background: var(--surface); border-right: 1px solid var(--border); padding: 1rem .9rem 1.5rem; overflow: auto; }
                main { min-width: 0; display: flex; flex-direction: column; align-items: flex-start; justify-content: center; padding: 2rem 2.5rem; }
                h1 { font-size: 1.8rem; font-weight: 650; margin: 0 0 .45rem; }
                h2 { font-size: .95rem; font-weight: 650; margin: .85rem 0 .35rem; color: var(--accent); }
                .lede, .meta { color: var(--muted); }
                .lede { margin: 0 0 1rem; max-width: 36rem; }
                .catalog { list-style: none; margin: 0; padding: 0; }
                .catalog ul { list-style: none; margin: 0; padding: 0; }
                .catalog li { position: relative; }
                .scenario { display: block; padding: .55rem .65rem; margin: .2rem 0; color: var(--text); text-decoration: none; border-left: 3px solid transparent; }
                .scenario:hover, .scenario:focus-visible { background: var(--raised); border-left-color: var(--accent); }
                .scenario .title { display: block; font-weight: 650; overflow-wrap: break-word; }
                .scenario .meta { display: block; font-size: .85rem; margin-top: .15rem; }
                .markdown { display: inline-block; margin: 0 0 .45rem .65rem; color: var(--muted); font-size: .85rem; }
                .stage { flex: 1; width: 100%; min-height: 12rem; border: 1px solid var(--border); background: #000; }
              </style>
            </head>
            <body>
              <div class="layout">
                <aside>
                  <p class="meta">Recordings</p>
                  <ul class="catalog">
            __NAV__
                  </ul>
                </aside>
                <main>
                  <h1>Walkthroughs</h1>
                  <p class="lede">Open a recording from the list — steps stay on the left, the frame updates on the right. Each walkthrough.html is one portable file. walkthrough.md is a separate Markdown story for editors and LLMs.</p>
                  <div class="stage" aria-hidden="true"></div>
                </main>
              </div>
              <script>
                function fileHref(href) {
                  try {
                    return new URL(href, location.href).href.replace(/^(file:\/\/\/[A-Za-z])%3A/i, "$1:");
                  } catch { return href; }
                }
                document.querySelectorAll("a.scenario, a.markdown").forEach((link) => {
                  link.setAttribute("href", fileHref(link.getAttribute("href")));
                  if (location.protocol !== "file:") return;
                  link.addEventListener("click", (event) => {
                    if (event.metaKey || event.ctrlKey || event.shiftKey || event.altKey || event.button !== 0) return;
                    event.preventDefault();
                    location.assign(link.href);
                  });
                });
              </script>
            </body>
            </html>
            """;
        return template.Replace("__NAV__", nav.ToString(), StringComparison.Ordinal);
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
