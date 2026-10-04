using System.Globalization;
using System.Net;
using System.Text;

namespace Novolis.Testing.Playwright;

/// <summary>Builds a static HTML document for one walkthrough. JavaScript only advances steps.</summary>
public static class PlaywrightWalkthroughPlayer
{
    /// <summary>Default hold when the manifest omits a usable duration.</summary>
    public const int DefaultFrameHoldMilliseconds = 4000;

    /// <summary>Renders a self-contained recording that can be read without JavaScript.</summary>
    /// <param name="manifest">Walkthrough written by <see cref="PlaywrightSession"/>.</param>
    /// <returns>HTML document.</returns>
    public static string Render(PlaywrightWalkthroughManifest manifest) =>
        Render(manifest, indexHref: null);

    /// <summary>Renders a self-contained recording that can be read without JavaScript.</summary>
    /// <param name="manifest">Walkthrough written by <see cref="PlaywrightSession"/>.</param>
    /// <param name="indexHref">Optional relative link back to the collection index.</param>
    /// <returns>HTML document.</returns>
    public static string Render(PlaywrightWalkthroughManifest manifest, string? indexHref)
    {
        ArgumentNullException.ThrowIfNull(manifest);
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
                h1, h2, h3, .step-title { font-family: Segoe UI, sans-serif; font-weight: 650; letter-spacing: 0; text-transform: none; overflow-wrap: break-word; }
                h1 { font-size: 1.65rem; margin: 0 0 .35rem; }
                h2 { font-size: 1.05rem; margin: 1.2rem 0 .4rem; }
                h3 { font-size: .95rem; margin: .85rem 0 .3rem; font-weight: 600; color: #3d3d3d; }
                .meta, .crumb { font-family: Segoe UI, sans-serif; color: #4a4a4a; margin: 0 0 .85rem; }
                .crumb a { color: #1d4b6e; }
                .part-label { font-family: Segoe UI, sans-serif; font-size: .85rem; color: #5a4a32; margin: 0 0 .25rem; letter-spacing: 0; text-transform: none; }
                .narration { background: #fff8ea; border: 1px solid #d8c7a2; padding: .75rem .9rem; margin: 0 0 1rem; }
                .outline { list-style: none; margin: 0; padding: 0; }
                .outline ol { list-style: none; margin: 0 0 .35rem; padding-left: 1.15rem; }
                .outline li { margin: .35rem 0; overflow-wrap: break-word; word-spacing: normal; }
                .section-heading { font-family: Segoe UI, sans-serif; font-weight: 650; margin: .55rem 0 .2rem; letter-spacing: 0; text-transform: none; overflow-wrap: break-word; }
                .outline a { color: #1d4b6e; text-decoration: none; }
                .outline a:hover, .outline a[aria-current="true"] { text-decoration: underline; }
                article.step { margin: 2.25rem 0 0; padding-top: .25rem; scroll-margin-top: 5rem; }
                article.step img { display: block; width: 100%; height: auto; background: #fff; border: 1px solid #d4cbb8; }
                .bar { position: sticky; bottom: 0; display: flex; flex-wrap: wrap; gap: .5rem; align-items: center; padding: .65rem 1rem; background: #efe8da; border-top: 1px solid #d4cbb8; font-family: Segoe UI, sans-serif; }
                button { background: #fff; color: #1a1a1a; border: 1px solid #8a7b62; padding: .4rem .75rem; cursor: pointer; }
                #play { background: #3d3224; border-color: #3d3224; color: #f7f4ee; }
                .kind-line.deviated, h3.deviated { color: #8a4b12; }
                .kind-line.planned, h3.planned { color: #2f5d2f; }
                .kind-line.closed, h3.closed { color: #555; }
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
                  __ARTICLES__
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
                function show(i) {
                  if (!articles.length) return;
                  index = (i + articles.length) % articles.length;
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
            .Replace("__ARTICLES__", BuildArticles(manifest, framePrefix: null), StringComparison.Ordinal)
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
                var kind = string.IsNullOrWhiteSpace(op.Kind) ? string.Empty : " " + Encode(op.Kind);
                text.Append("<li class=\"section")
                    .Append(kind)
                    .Append("\">");
                if (!string.IsNullOrWhiteSpace(op.Heading))
                {
                    text.Append("<div class=\"section-heading\">")
                        .Append(Encode(op.Heading))
                        .Append("</div>");
                }

                text.Append("<ol>");
            }
            else if (op.Action == "close")
            {
                text.Append("</ol></li>");
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

    /// <summary>Renders step articles for embedding in the collection overview.</summary>
    public static string RenderArticles(PlaywrightWalkthroughManifest manifest, string? framePrefix) =>
        BuildArticles(manifest, framePrefix);

    private static string BuildArticles(PlaywrightWalkthroughManifest manifest, string? framePrefix)
    {
        var text = new StringBuilder();
        foreach (var step in manifest.Steps)
        {
            var kindLine = step.Kind switch
            {
                "deviated" => "This flow departed from the usual clock.",
                "planned" => "This day followed the usual clock.",
                "closed" => "The shop was shut. Not a gap.",
                _ => string.Empty,
            };
            var path = PlaywrightWalkthroughOutline.PathOf(step);
            text.Append("<article class=\"step\" id=\"step-")
                .Append(step.Index.ToString(CultureInfo.InvariantCulture))
                .Append("\" style=\"padding-left:")
                .Append((path.Count * 1.15).ToString("0.##", CultureInfo.InvariantCulture))
                .Append("rem\">");
            foreach (var heading in path.Where(heading => !string.IsNullOrWhiteSpace(heading)))
            {
                text.Append("<p class=\"part-label\">").Append(Encode(heading)).Append("</p>");
            }

            text.Append("<h2 class=\"step-title\">")
                .Append(step.Index.ToString(CultureInfo.InvariantCulture))
                .Append(". ")
                .Append(Encode(step.Name))
                .Append("</h2>");
            if (!string.IsNullOrWhiteSpace(kindLine))
            {
                text.Append("<p class=\"kind-line ")
                    .Append(Encode(step.Kind))
                    .Append("\">")
                    .Append(Encode(kindLine))
                    .Append("</p>");
            }

            if (!string.IsNullOrWhiteSpace(step.Narration))
            {
                text.Append("<p class=\"narration\">").Append(Encode(step.Narration)).Append("</p>");
            }

            if (!string.IsNullOrWhiteSpace(step.FrameFile))
            {
                var frame = step.FrameFile.Replace('\\', '/');
                if (!string.IsNullOrWhiteSpace(framePrefix))
                {
                    frame = framePrefix.TrimEnd('/') + "/" + frame.TrimStart('/');
                }

                text.Append("<img src=\"")
                    .Append(Encode(frame))
                    .Append("\" alt=\"")
                    .Append(Encode(step.Name))
                    .Append("\"/>");
            }

            text.Append("</article>");
        }

        return text.ToString();
    }

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
