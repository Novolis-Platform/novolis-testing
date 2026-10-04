using System.Globalization;
using System.Net;
using System.Text;

namespace Novolis.Testing.Playwright;

/// <summary>
/// Builds a static HTML document for one scenario.
/// JavaScript only advances steps. The document stays readable without it.
/// </summary>
public static class PlaywrightWalkthroughPlayer
{
    /// <summary>Default hold when the manifest omits a usable duration.</summary>
    public const int DefaultFrameHoldMilliseconds = 4000;

    /// <summary>Renders a recording. Frame paths stay relative unless a resolver inlines them.</summary>
    /// <param name="manifest">Walkthrough written by <see cref="PlaywrightSession"/>.</param>
    /// <returns>HTML document.</returns>
    public static string Render(PlaywrightWalkthroughManifest manifest) =>
        Render(manifest, indexHref: null, resolveAsset: null);

    /// <summary>Renders a recording with a crumb back to the collection index.</summary>
    /// <param name="manifest">Walkthrough written by <see cref="PlaywrightSession"/>.</param>
    /// <param name="indexHref">Optional relative link back to the collection index.</param>
    /// <returns>HTML document.</returns>
    public static string Render(PlaywrightWalkthroughManifest manifest, string? indexHref) =>
        Render(manifest, indexHref, resolveAsset: null);

    /// <summary>
    /// Renders a recording. <paramref name="resolveAsset"/> compiles frame paths (data URIs or relative files).
    /// The renderer does not read the filesystem.
    /// </summary>
    /// <param name="manifest">Walkthrough written by <see cref="PlaywrightSession"/>.</param>
    /// <param name="indexHref">Optional relative link back to the collection index.</param>
    /// <param name="resolveAsset">Maps a manifest asset path to an <c>img</c> or media <c>src</c>.</param>
    /// <returns>HTML document.</returns>
    public static string Render(
        PlaywrightWalkthroughManifest manifest,
        string? indexHref,
        Func<string, string>? resolveAsset)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        resolveAsset ??= static path => path.Replace('\\', '/');
        var hold = manifest.FrameHoldMilliseconds > 0
            ? manifest.FrameHoldMilliseconds
            : DefaultFrameHoldMilliseconds;
        var title = PlaywrightWalkthroughTitle.Display(manifest.Title);
        var partCount = PlaywrightWalkthroughOutline.SectionCount(manifest.Steps);
        var raw = string.IsNullOrWhiteSpace(manifest.RawCaptureFile)
            ? string.Empty
            : $"""
              <details class="raw">
                <summary>Raw capture (real-time, usually too fast to read)</summary>
                <video controls muted src="{Encode(manifest.RawCaptureFile)}"></video>
              </details>
              """;
        var indexLink = string.IsNullOrWhiteSpace(indexHref)
            ? string.Empty
            : $"<p class=\"crumb\"><a href=\"{Encode(indexHref)}\">All walkthroughs</a></p>";
        const string template = """
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8"/>
              <meta name="viewport" content="width=device-width, initial-scale=1"/>
              <title>__TITLE__</title>
              <style>
                body { font-family: Georgia, "Times New Roman", serif; margin: 0; color: #1a1a1a; background: #f7f4ee; line-height: 1.45; }
                .layout { display: grid; grid-template-columns: minmax(16rem, 22rem) 1fr; min-height: 100vh; }
                @media (max-width: 900px) { .layout { grid-template-columns: 1fr; } }
                aside { background: #efe8da; border-right: 1px solid #d4cbb8; padding: 1.25rem 1.1rem 6rem; overflow: auto; }
                main { padding: 1.25rem 1.5rem 6rem; max-width: 52rem; }
                h1, h2, .step-title, summary { font-family: Segoe UI, sans-serif; font-weight: 650; letter-spacing: 0; text-transform: none; overflow-wrap: break-word; }
                h1 { font-size: 1.65rem; margin: 0 0 .35rem; }
                h2, .step-title { font-size: 1.15rem; margin: 0 0 .4rem; }
                .meta, .crumb { font-family: Segoe UI, sans-serif; color: #4a4a4a; margin: 0 0 .85rem; }
                .crumb a { color: #1d4b6e; }
                .narration { background: #fff8ea; border: 1px solid #d8c7a2; padding: .75rem .9rem; margin: 0 0 1rem; }
                .outline { list-style: none; margin: 0; padding: 0; }
                .outline ol { list-style: none; margin: 0 0 .35rem; padding-left: 1.15rem; }
                .outline li { margin: .35rem 0; overflow-wrap: break-word; }
                .outline a { color: #1d4b6e; text-decoration: none; }
                .outline a:hover, .outline a[aria-current="true"] { text-decoration: underline; }
                .outline summary { cursor: pointer; margin: .45rem 0 .2rem; }
                details.section { margin: 1.5rem 0 0; padding-left: .15rem; }
                details.section > summary { cursor: pointer; font-size: 1.05rem; margin: 0 0 .65rem; }
                .section-indent { margin-left: 1.15rem; }
                article.step { margin: 1.25rem 0 0; scroll-margin-top: 5rem; }
                article.step img { display: block; width: 100%; height: auto; background: #fff; border: 1px solid #d4cbb8; }
                .kind-badge { display: inline-block; font-family: Segoe UI, sans-serif; font-size: .72rem; font-weight: 650; margin-left: .45rem; padding: .08rem .4rem; border: 1px solid currentColor; vertical-align: middle; }
                .kind-badge.deviated { color: #8a4b12; }
                .kind-badge.planned { color: #2f5d2f; }
                .kind-badge.closed { color: #555; }
                .bar { position: sticky; bottom: 0; display: flex; flex-wrap: wrap; gap: .5rem; align-items: center; padding: .65rem 1rem; background: #efe8da; border-top: 1px solid #d4cbb8; font-family: Segoe UI, sans-serif; }
                button { background: #fff; color: #1a1a1a; border: 1px solid #8a7b62; padding: .4rem .75rem; cursor: pointer; }
                #play { background: #3d3224; border-color: #3d3224; color: #f7f4ee; }
                .raw { margin-top: 1.25rem; color: #555; }
                .raw video { width: min(100%, 720px); margin-top: .5rem; }
              </style>
            </head>
            <body>
              <div class="layout">
                <aside>
                  __INDEX__
                  <p class="meta">__COUNTS__</p>
                  <nav class="outline" aria-label="Steps">__OUTLINE__</nav>
                </aside>
                <main>
                  __INDEX__
                  <h1>__TITLE__</h1>
                  <p class="meta">__INTRO__</p>
                  __SCENARIO__
                  __RAW__
                </main>
              </div>
              <div class="bar">
                <button type="button" id="prev">Previous</button>
                <button type="button" id="play" aria-pressed="false">Play scenario</button>
                <button type="button" id="next">Next</button>
                <span class="meta" id="progress"></span>
              </div>
              <script>
                const articles = Array.from(document.querySelectorAll("article.step"));
                const links = Array.from(document.querySelectorAll("nav.outline a[href^='#step-']"));
                const hold = __HOLD__;
                const play = document.getElementById("play");
                const progress = document.getElementById("progress");
                let index = 0;
                let timer = 0;
                let running = false;
                function reveal(article) {
                  let node = article.parentElement;
                  while (node) {
                    if (node.tagName === "DETAILS") node.open = true;
                    node = node.parentElement;
                  }
                }
                function show(i) {
                  if (!articles.length) return;
                  index = (i + articles.length) % articles.length;
                  reveal(articles[index]);
                  articles[index].scrollIntoView({ behavior: "smooth", block: "start" });
                  progress.textContent = "Step " + (index + 1) + " of " + articles.length;
                  for (const link of links) {
                    link.setAttribute("aria-current", link.getAttribute("href") === "#step-" + (index + 1) ? "true" : "false");
                  }
                }
                function setRunning(on) {
                  running = on && articles.length > 1;
                  play.textContent = running ? "Pause" : "Play scenario";
                  play.setAttribute("aria-pressed", running ? "true" : "false");
                  window.clearInterval(timer);
                  if (running) timer = window.setInterval(() => show(index + 1), hold);
                }
                document.getElementById("prev").addEventListener("click", () => { show(index - 1); setRunning(false); });
                document.getElementById("next").addEventListener("click", () => { show(index + 1); setRunning(false); });
                play.addEventListener("click", () => setRunning(!running));
                document.addEventListener("keydown", (event) => {
                  if (event.key === "ArrowRight") { show(index + 1); setRunning(false); }
                  if (event.key === "ArrowLeft") { show(index - 1); setRunning(false); }
                  if (event.key === " ") { event.preventDefault(); setRunning(!running); }
                });
                show(0);
              </script>
            </body>
            </html>
            """;
        return template
            .Replace("__TITLE__", Encode(title), StringComparison.Ordinal)
            .Replace("__INDEX__", indexLink, StringComparison.Ordinal)
            .Replace(
                "__COUNTS__",
                partCount.ToString(CultureInfo.InvariantCulture) + " sections · " +
                manifest.Steps.Count.ToString(CultureInfo.InvariantCulture) + " steps",
                StringComparison.Ordinal)
            .Replace("__OUTLINE__", BuildOutline(manifest), StringComparison.Ordinal)
            .Replace(
                "__INTRO__",
                "Read down the page, or use Play / Next. " +
                (hold / 1000.0).ToString("0.#", CultureInfo.InvariantCulture) +
                "s per step when playing.",
                StringComparison.Ordinal)
            .Replace("__SCENARIO__", BuildScenario(manifest, resolveAsset), StringComparison.Ordinal)
            .Replace("__RAW__", raw, StringComparison.Ordinal)
            .Replace("__HOLD__", hold.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    private static string BuildOutline(PlaywrightWalkthroughManifest manifest)
    {
        var text = new StringBuilder();
        text.Append("<ol class=\"outline-root\">");
        foreach (var op in PlaywrightWalkthroughOutline.Build(manifest.Steps))
        {
            if (op.Action == "open")
            {
                text.Append("<li class=\"section\">");
                if (!string.IsNullOrWhiteSpace(op.Heading))
                {
                    text.Append("<details open><summary>")
                        .Append(Encode(op.Heading))
                        .Append(KindBadge(op.Kind))
                        .Append("</summary>");
                }

                text.Append("<ol>");
            }
            else if (op.Action == "close")
            {
                text.Append("</ol>");
                if (!string.IsNullOrWhiteSpace(op.Heading))
                {
                    text.Append("</details>");
                }

                text.Append("</li>");
            }
            else if (op.Step is { } step)
            {
                text.Append("<li class=\"step\"><a href=\"#step-")
                    .Append(step.Index.ToString(CultureInfo.InvariantCulture))
                    .Append("\">")
                    .Append(Encode(step.Name))
                    .Append("</a></li>");
            }
        }

        text.Append("</ol>");
        return text.ToString();
    }

    private static string BuildScenario(PlaywrightWalkthroughManifest manifest, Func<string, string> resolveAsset)
    {
        var text = new StringBuilder();
        foreach (var op in PlaywrightWalkthroughOutline.Build(manifest.Steps))
        {
            if (op.Action == "open")
            {
                if (string.IsNullOrWhiteSpace(op.Heading))
                {
                    text.Append("<div class=\"section-indent\">");
                    continue;
                }

                text.Append("<details class=\"section\" open><summary>")
                    .Append(Encode(op.Heading))
                    .Append(KindBadge(op.Kind))
                    .Append("</summary>");
            }
            else if (op.Action == "close")
            {
                text.Append(string.IsNullOrWhiteSpace(op.Heading) ? "</div>" : "</details>");
            }
            else if (op.Step is { } step)
            {
                AppendStep(text, step, resolveAsset);
            }
        }

        return text.ToString();
    }

    private static void AppendStep(
        StringBuilder text,
        PlaywrightWalkthroughStep step,
        Func<string, string> resolveAsset)
    {
        text.Append("<article class=\"step\" id=\"step-")
            .Append(step.Index.ToString(CultureInfo.InvariantCulture))
            .Append("\">");
        text.Append("<h2 class=\"step-title\">")
            .Append(step.Index.ToString(CultureInfo.InvariantCulture))
            .Append(". ")
            .Append(Encode(step.Name))
            .Append("</h2>");
        if (!string.IsNullOrWhiteSpace(step.Narration))
        {
            text.Append("<p class=\"narration\">").Append(Encode(step.Narration)).Append("</p>");
        }

        if (!string.IsNullOrWhiteSpace(step.FrameFile))
        {
            var source = resolveAsset(step.FrameFile.Replace('\\', '/'));
            text.Append("<img src=\"")
                .Append(Encode(source))
                .Append("\" alt=\"")
                .Append(Encode(step.Name))
                .Append("\"/>");
        }

        text.Append("</article>");
    }

    private static string KindBadge(string? kind)
    {
        if (string.IsNullOrWhiteSpace(kind))
        {
            return string.Empty;
        }

        var label = kind switch
        {
            "deviated" => "Deviated",
            "planned" => "Planned",
            "closed" => "Closed",
            _ => kind,
        };
        return "<span class=\"kind-badge " + Encode(kind) + "\">" + Encode(label) + "</span>";
    }

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
