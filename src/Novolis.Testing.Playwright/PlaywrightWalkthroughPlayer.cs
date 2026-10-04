using System.Globalization;
using System.Net;
using System.Text.Json;

namespace Novolis.Testing.Playwright;

/// <summary>Builds the HTML scenario recording from parts and step frames.</summary>
public static class PlaywrightWalkthroughPlayer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Default hold when the manifest omits a usable duration.</summary>
    public const int DefaultFrameHoldMilliseconds = 4000;

    /// <summary>Renders a self-contained recording that steps through PNG frames.</summary>
    /// <param name="manifest">Walkthrough written by <see cref="PlaywrightSession"/>.</param>
    /// <returns>HTML document.</returns>
    public static string Render(PlaywrightWalkthroughManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var hold = manifest.FrameHoldMilliseconds > 0
            ? manifest.FrameHoldMilliseconds
            : DefaultFrameHoldMilliseconds;
        var framesJson = JsonSerializer.Serialize(manifest.Steps, JsonOptions);
        var title = Encode(manifest.Title);
        var partCount = manifest.Steps.Select(step => step.Part).Distinct(StringComparer.Ordinal).Count();
        var raw = string.IsNullOrWhiteSpace(manifest.RawCaptureFile)
            ? string.Empty
            : $"""
              <details class="raw">
                <summary>Raw capture (real-time, usually too fast to read)</summary>
                <video controls muted src="{Encode(manifest.RawCaptureFile)}"></video>
              </details>
              """;
        return
            $$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8"/>
              <meta name="viewport" content="width=device-width, initial-scale=1"/>
              <title>{{title}}</title>
              <style>
                :root { color-scheme: dark; }
                * { box-sizing: border-box; }
                body { font-family: Segoe UI, sans-serif; margin: 0; background: #141414; color: #f2f2f2; }
                .layout { display: grid; grid-template-columns: minmax(16rem, 22rem) 1fr; min-height: 100vh; }
                @media (max-width: 900px) { .layout { grid-template-columns: 1fr; } }
                aside { background: #1c1c1c; border-right: 1px solid #2e2e2e; padding: 1.25rem; overflow: auto; }
                main { padding: 1.25rem 1.5rem 2rem; }
                h1 { font-size: 1.35rem; margin: 0 0 .35rem; }
                .meta { color: #b8b8b8; margin: 0 0 1rem; }
                .part-label { text-transform: uppercase; letter-spacing: .08em; font-size: .72rem; color: #c4a574; margin: 0 0 .25rem; }
                .step-title { font-size: 1.15rem; margin: 0 0 .5rem; }
                .narration { background: #231f18; border: 1px solid #5c4a2e; color: #f4e6c8; padding: .85rem 1rem; margin: 0 0 1rem; line-height: 1.45; }
                .recording { background: #0d0d0d; border: 1px solid #2a2a2a; }
                .recording img { display: block; width: 100%; height: auto; background: #111; }
                .bar { display: flex; flex-wrap: wrap; gap: .5rem; align-items: center; padding: .75rem 1rem; border-top: 1px solid #2a2a2a; }
                button { background: #2a2a2a; color: #eee; border: 1px solid #444; padding: .4rem .75rem; cursor: pointer; }
                #play { background: #3d3224; border-color: #c4a574; }
                button[aria-pressed="true"] { border-color: #c4a574; }
                .outline h2 { font-size: .95rem; font-weight: 650; letter-spacing: 0; color: #e6c48a; margin: 1.1rem 0 .4rem; line-height: 1.35; }
                .outline h2:first-child { margin-top: 0; }
                .outline { list-style: none; margin: 0; padding: 0; }
                .outline button { width: 100%; text-align: left; margin: 0 0 .45rem; white-space: normal; overflow-wrap: anywhere; line-height: 1.35; letter-spacing: 0; word-spacing: .04em; }
                .outline h3 { font-size: .88rem; font-weight: 600; margin: .7rem 0 .3rem .15rem; color: #d0d0d0; line-height: 1.35; letter-spacing: 0; }
                .outline h3.deviated, .kind-line.deviated, .flow-label.deviated { color: #e0a060; }
                .outline h3.planned, .kind-line.planned { color: #8aab8a; }
                .outline h3.closed, .kind-line.closed { color: #9a9a9a; }
                .outline button.deviated { border-left: 3px solid #e0a060; }
                .outline button.planned { border-left: 3px solid #8aab8a; }
                .outline button.closed { border-left: 3px solid #666; }
                .flow-label { font-size: .85rem; margin: 0 0 .35rem; color: #c4a574; }
                .kind-line { margin: 0 0 .75rem; font-size: .9rem; }
                .raw { margin-top: 1.25rem; color: #999; }
                .raw video { width: min(100%, 720px); margin-top: .5rem; }
              </style>
            </head>
            <body>
              <div class="layout">
                <aside>
                  <p class="meta">{{partCount}} parts · {{manifest.Steps.Count}} steps</p>
                  <div id="outline" class="outline"></div>
                </aside>
                <main>
                  <h1>{{title}}</h1>
                  <p class="meta">Scenario recording. Starts paused. {{(hold / 1000.0).ToString("0.#", CultureInfo.InvariantCulture)}}s per step.</p>
                  <p class="part-label" id="part"></p>
                  <p class="flow-label" id="flow" hidden></p>
                  <h2 class="step-title" id="stepTitle"></h2>
                  <p class="kind-line" id="kind" hidden></p>
                  <p class="narration" id="narration">Play when you want to walk the scenario. The outline on the left is the story.</p>
                  <section class="recording" aria-label="Walkthrough recording">
                    <img id="frame" alt=""/>
                    <div class="bar">
                      <button type="button" id="prev">Previous</button>
                      <button type="button" id="play" aria-pressed="false">Play scenario</button>
                      <button type="button" id="next">Next</button>
                      <span class="meta" id="progress"></span>
                    </div>
                  </section>
                  {{raw}}
                </main>
              </div>
              <script>
                const frames = {{framesJson}};
                const hold = {{hold}};
                const img = document.getElementById("frame");
                const partEl = document.getElementById("part");
                const flowEl = document.getElementById("flow");
                const titleEl = document.getElementById("stepTitle");
                const kindEl = document.getElementById("kind");
                const narrationEl = document.getElementById("narration");
                const progressEl = document.getElementById("progress");
                const play = document.getElementById("play");
                const outline = document.getElementById("outline");
                let index = 0;
                let timer = 0;
                let running = false;

                function show(i) {
                  if (!frames.length) {
                    titleEl.textContent = "No frames recorded.";
                    return;
                  }
                  index = (i + frames.length) % frames.length;
                  const step = frames[index];
                  img.src = step.frameFile;
                  img.alt = step.name;
                  partEl.textContent = "Part · " + (step.part || "Scenario");
                  const path = step.flowPath || step.flow || "";
                  flowEl.textContent = path;
                  flowEl.hidden = !path;
                  flowEl.className = "flow-label " + (step.kind || "");
                  titleEl.textContent = step.name;
                  const kindLine = step.kind === "deviated"
                    ? "This flow departed from the usual clock."
                    : step.kind === "planned"
                      ? "This day followed the usual clock."
                      : step.kind === "closed"
                        ? "The shop was shut. Not a gap."
                        : "";
                  kindEl.textContent = kindLine;
                  kindEl.hidden = !kindLine;
                  kindEl.className = "kind-line " + (step.kind || "");
                  narrationEl.textContent = step.narration || step.name;
                  progressEl.textContent = "Step " + (index + 1) + " of " + frames.length;
                  for (const button of outline.querySelectorAll("button[data-index]")) {
                    button.setAttribute("aria-pressed", button.dataset.index === String(index) ? "true" : "false");
                  }
                }

                function setRunning(on) {
                  running = on && frames.length > 1;
                  play.textContent = running ? "Pause" : "Play scenario";
                  play.setAttribute("aria-pressed", running ? "true" : "false");
                  window.clearInterval(timer);
                  if (running) {
                    timer = window.setInterval(() => show(index + 1), hold);
                  }
                }

                let lastPart = "";
                let lastGroup = "";
                frames.forEach((step, i) => {
                  if (step.part && step.part !== lastPart) {
                    const heading = document.createElement("h2");
                    heading.textContent = step.part;
                    outline.appendChild(heading);
                    lastPart = step.part;
                    lastGroup = "";
                  }
                  const group = step.flowPath || step.flow || "";
                  if (group && group !== lastGroup) {
                    const flowHead = document.createElement("h3");
                    flowHead.textContent = group;
                    flowHead.className = step.kind || "";
                    outline.appendChild(flowHead);
                    lastGroup = group;
                  }
                  const button = document.createElement("button");
                  button.type = "button";
                  button.textContent = step.index + ". " + step.name;
                  button.dataset.index = String(i);
                  button.dataset.kind = step.kind || "";
                  button.dataset.depth = String(step.depth || 0);
                  const depth = Number(step.depth || 0);
                  button.style.marginLeft = (depth * 12) + "px";
                  if (step.kind) {
                    button.classList.add(step.kind);
                  }
                  button.addEventListener("click", () => { show(i); setRunning(false); });
                  outline.appendChild(button);
                });

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
    }

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
