using System.Globalization;
using System.Net;
using System.Text;

namespace Novolis.Testing.Playwright;

/// <summary>
/// Builds a static HTML player for one scenario.
/// The outline stays on the left. One frame fills the right and updates as you play.
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
        var crumbs = new StringBuilder("<p class=\"crumb\">");
        if (!string.IsNullOrWhiteSpace(indexHref))
        {
            crumbs.Append("<a id=\"catalog\" href=\"")
                .Append(Encode(indexHref))
                .Append("\">All walkthroughs</a><span> · </span>");
        }

        crumbs.Append("<a id=\"markdown\" href=\"walkthrough.md\">Markdown</a></p>");
        const string template = """
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8"/>
              <meta name="viewport" content="width=device-width, initial-scale=1"/>
              <title>__TITLE__</title>
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
                  --action: #8F37FF;
                  --on-action: #EFFDFF;
                }
                * { box-sizing: border-box; }
                html, body { height: 100%; margin: 0; }
                body { font-family: "Segoe UI", sans-serif; color: var(--text); background: var(--bg); }
                .layout { display: grid; grid-template-columns: minmax(16rem, 22rem) 1fr; grid-template-rows: 1fr auto; height: 100vh; }
                @media (max-width: 900px) {
                  .layout { grid-template-columns: 1fr; grid-template-rows: minmax(10rem, 32vh) 1fr auto; }
                  aside { grid-row: 1; border-right: 0; border-bottom: 1px solid var(--border); }
                }
                aside { grid-row: 1 / span 2; background: var(--surface); border-right: 1px solid var(--border); padding: 1rem .9rem 1.25rem; overflow: auto; }
                main { min-width: 0; min-height: 0; display: flex; flex-direction: column; padding: 1rem 1.25rem 0; }
                h1, h2, .step-title, summary { font-weight: 650; letter-spacing: 0; text-transform: none; overflow-wrap: break-word; }
                h1 { font-size: 1.35rem; margin: 0 0 .35rem; }
                .step-title { font-size: 1.05rem; margin: 0 0 .35rem; }
                .meta, .crumb { color: var(--muted); margin: 0 0 .75rem; }
                .crumb a, .outline a { color: var(--text); text-decoration: none; }
                .crumb a:hover { color: var(--accent); }
                .narration { margin: 0 0 .7rem; color: var(--muted); }
                .outline { list-style: none; margin: 0; padding: 0; }
                .outline ol { list-style: none; margin: 0; padding-left: .85rem; }
                .outline li { margin: .15rem 0; overflow-wrap: break-word; }
                .outline a { display: block; padding: .35rem .55rem; border-left: 3px solid transparent; }
                .outline a:hover { background: var(--raised); }
                .outline a[aria-current="true"] { background: var(--raised); color: var(--accent); border-left-color: var(--accent); }
                .outline summary { cursor: pointer; margin: .35rem 0 .15rem; }
                .stage { flex: 1; min-height: 0; position: relative; }
                article.step { display: none; position: absolute; inset: 0; flex-direction: column; min-height: 0; }
                html:not(.js) .stage:not(:has(article.step:target)) article.step:first-of-type,
                html:not(.js) .stage article.step:target,
                .stage article.step.is-current { display: flex; }
                .viewport { flex: 1; min-height: 0; background: #000; border: 1px solid var(--border); display: flex; align-items: center; justify-content: center; }
                .viewport img { display: block; max-width: 100%; max-height: 100%; width: auto; height: auto; object-fit: contain; }
                .kind-badge { display: inline-block; font-size: .72rem; font-weight: 650; margin-left: .45rem; padding: .08rem .4rem; border: 1px solid currentColor; vertical-align: middle; }
                .kind-badge.deviated { color: #B246FF; }
                .kind-badge.planned { color: var(--accent); }
                .kind-badge.closed { color: var(--muted); }
                .bar { display: flex; flex-wrap: wrap; gap: .5rem; align-items: center; padding: .65rem 1.25rem; background: var(--surface); border-top: 1px solid var(--border); }
                button { font: inherit; background: var(--fill); color: var(--on-fill); border: 1px solid var(--fill); padding: .45rem .85rem; cursor: pointer; }
                #play { background: var(--action); border-color: var(--action); color: var(--on-action); }
                .raw { margin-top: 1rem; color: var(--muted); }
                .raw video { width: min(100%, 720px); margin-top: .5rem; }
              </style>
            </head>
            <body>
              <div class="layout">
                <aside>
                  __INDEX__
                  <h1>__TITLE__</h1>
                  <p class="meta">__COUNTS__</p>
                  <nav class="outline" aria-label="Steps">__OUTLINE__</nav>
                  __RAW__
                </aside>
                <main>
                  <div class="stage" id="stage">__SCENARIO__</div>
                </main>
                <div class="bar">
                  <button type="button" id="prev">Previous</button>
                  <button type="button" id="play" aria-pressed="false">Play scenario</button>
                  <button type="button" id="next">Next</button>
                  <span class="meta" id="progress"></span>
                </div>
              </div>
              <script>
                document.documentElement.classList.add("js");
                function fileHref(href) {
                  try {
                    return new URL(href, location.href).href.replace(/^(file:\/\/\/[A-Za-z])%3A/i, "$1:");
                  } catch { return href; }
                }
                document.querySelectorAll("#catalog, #markdown").forEach((link) => {
                  link.setAttribute("href", fileHref(link.getAttribute("href")));
                  if (location.protocol !== "file:") return;
                  link.addEventListener("click", (event) => {
                    if (event.metaKey || event.ctrlKey || event.shiftKey || event.altKey || event.button !== 0) return;
                    event.preventDefault();
                    location.assign(link.href);
                  });
                });
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
                  const href = "#" + article.id;
                  for (const link of links) {
                    if (link.getAttribute("href") === href) {
                      let parent = link.parentElement;
                      while (parent) {
                        if (parent.tagName === "DETAILS") parent.open = true;
                        parent = parent.parentElement;
                      }
                    }
                  }
                }
                function show(i) {
                  if (!articles.length) return;
                  index = Math.max(0, Math.min(i, articles.length - 1));
                  articles.forEach((article, n) => article.classList.toggle("is-current", n === index));
                  reveal(articles[index]);
                  progress.textContent = "Step " + (index + 1) + " of " + articles.length;
                  const current = "#step-" + (index + 1);
                  for (const link of links) {
                    link.setAttribute("aria-current", link.getAttribute("href") === current ? "true" : "false");
                  }
                  if (articles[index].id && location.hash !== "#" + articles[index].id) {
                    history.replaceState(null, "", "#" + articles[index].id);
                  }
                }
                function setRunning(on) {
                  running = on && articles.length > 1 && index < articles.length - 1;
                  play.textContent = running ? "Pause" : "Play scenario";
                  play.setAttribute("aria-pressed", running ? "true" : "false");
                  window.clearInterval(timer);
                  if (running) {
                    timer = window.setInterval(() => {
                      if (index + 1 >= articles.length) { setRunning(false); return; }
                      show(index + 1);
                    }, hold);
                  }
                }
                document.getElementById("prev").addEventListener("click", () => { show(index - 1); setRunning(false); });
                document.getElementById("next").addEventListener("click", () => { show(index + 1); setRunning(false); });
                play.addEventListener("click", () => setRunning(!running));
                for (const link of links) {
                  link.addEventListener("click", (event) => {
                    event.preventDefault();
                    const href = link.getAttribute("href") || "";
                    const i = articles.findIndex((article) => "#" + article.id === href);
                    if (i >= 0) { show(i); setRunning(false); }
                  });
                }
                document.addEventListener("keydown", (event) => {
                  if (event.key === "ArrowRight") { show(index + 1); setRunning(false); }
                  if (event.key === "ArrowLeft") { show(index - 1); setRunning(false); }
                  if (event.key === " ") { event.preventDefault(); setRunning(!running); }
                });
                const fromHash = articles.findIndex((article) => "#" + article.id === location.hash);
                show(fromHash >= 0 ? fromHash : 0);
              </script>
            </body>
            </html>
            """;
        return template
            .Replace("__TITLE__", Encode(title), StringComparison.Ordinal)
            .Replace("__INDEX__", crumbs.ToString(), StringComparison.Ordinal)
            .Replace(
                "__COUNTS__",
                partCount.ToString(CultureInfo.InvariantCulture) + " sections · " +
                manifest.Steps.Count.ToString(CultureInfo.InvariantCulture) + " steps",
                StringComparison.Ordinal)
            .Replace("__OUTLINE__", BuildOutline(manifest), StringComparison.Ordinal)
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
                    text.Append("<details class=\"section\" open><summary>")
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
        var first = true;
        foreach (var step in manifest.Steps)
        {
            AppendStep(text, step, resolveAsset, first);
            first = false;
        }

        return text.ToString();
    }

    private static void AppendStep(
        StringBuilder text,
        PlaywrightWalkthroughStep step,
        Func<string, string> resolveAsset,
        bool current)
    {
        text.Append("<article class=\"step");
        if (current)
        {
            text.Append(" is-current");
        }

        text.Append("\" id=\"step-")
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
            text.Append("<div class=\"viewport\"><img src=\"")
                .Append(Encode(source))
                .Append("\" alt=\"")
                .Append(Encode(step.Name))
                .Append("\"/></div>");
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
