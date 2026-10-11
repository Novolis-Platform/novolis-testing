using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Novolis.Testing.Timeline;

/// <summary>Renders a portable HTML player for one timeline.</summary>
public static class TimelineHtmlRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>Renders a timeline using relative asset paths.</summary>
    public static string Render(
        TimelineManifest manifest,
        IReadOnlyList<TimelineEvent> events,
        IReadOnlyList<object> telemetry,
        IReadOnlyList<string> logs,
        Func<string, string> resolveAsset) =>
        Render(manifest, events, telemetry, logs, indexHref: null, resolveAsset);

    /// <summary>
    /// Renders a timeline and optionally links back to a collection index.
    /// The renderer does not read the filesystem.
    /// </summary>
    public static string Render(
        TimelineManifest manifest,
        IReadOnlyList<TimelineEvent> events,
        IReadOnlyList<object> telemetry,
        IReadOnlyList<string> logs,
        string? indexHref,
        Func<string, string> resolveAsset)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(telemetry);
        ArgumentNullException.ThrowIfNull(logs);
        ArgumentNullException.ThrowIfNull(resolveAsset);
        var failed = events.Count(item => Is(item.Status, "failed"));
        var skipped = events.Count(item =>
            Is(item.Status, "skipped") || Is(item.Status, "single-monitor"));
        var recorded = events.Count(item =>
            Is(item.Status, "passed")
            || Is(item.Status, "captured")
            || Is(item.Status, "negotiated"));
        var runStatus = failed > 0
            ? "failed"
            : events.Count == 0
                ? "no-events"
                : "evidence-captured";

        var indexLink = string.IsNullOrWhiteSpace(indexHref)
            ? string.Empty
            : $"<a href=\"{Encode(indexHref)}\">All timelines</a>";
        var baseUrl = string.IsNullOrWhiteSpace(manifest.BaseUrl)
            ? string.Empty
            : $"<p class=\"meta\">Base URL: {Encode(manifest.BaseUrl)}</p>";
        var rawCapture = string.IsNullOrWhiteSpace(manifest.RawCaptureFile)
            ? string.Empty
            : $"""
              <details open><summary>Raw capture</summary><video controls muted src="{Encode(resolveAsset(manifest.RawCaptureFile))}"></video></details>
              """;
        var traceLink = string.IsNullOrWhiteSpace(manifest.TraceFile)
            ? string.Empty
            : $"<a href=\"{Encode(manifest.TraceFile)}\">{Encode(manifest.TraceFile)}</a>";

        const string template = """
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8"/>
              <meta name="viewport" content="width=device-width, initial-scale=1"/>
              <title>__TITLE__</title>
              <style>
                :root {
                  --bg: #07131c;
                  --surface: #0d202b;
                  --raised: #12303d;
                  --border: #28566a;
                  --text: #eef8fb;
                  --muted: #a8c3cc;
                  --accent: #6fe4df;
                  --action: #2aa9a3;
                  --success: #83df9c;
                  --warning: #f0c36d;
                  --danger: #ff8e8e;
                }
                * { box-sizing: border-box; }
                html, body { min-height: 100%; margin: 0; }
                body {
                  color: var(--text);
                  background: var(--bg);
                  font-family: "Segoe UI", sans-serif;
                  line-height: 1.4;
                }
                a { color: var(--accent); }
                button {
                  color: var(--text);
                  background: var(--action);
                  border: 1px solid var(--accent);
                  padding: .45rem .8rem;
                  font: inherit;
                  cursor: pointer;
                }
                .shell { min-height: 100vh; display: grid; grid-template-columns: minmax(18rem, 24rem) 1fr; }
                aside { padding: 1rem; background: var(--surface); border-right: 1px solid var(--border); overflow: auto; }
                main { min-width: 0; padding: 1rem 1.25rem 2rem; }
                h1, h2, h3 { line-height: 1.15; margin: 0; }
                h1 { font-size: 1.35rem; }
                h2 { font-size: 1.2rem; }
                h3 { font-size: .98rem; }
                .eyebrow, .meta, .muted { color: var(--muted); }
                .eyebrow { margin: 0 0 .35rem; font-size: .75rem; letter-spacing: .08em; text-transform: uppercase; }
                .lede { margin: .45rem 0 1rem; color: var(--muted); }
                .summary, .card, details { background: var(--surface); border: 1px solid var(--border); }
                .summary { padding: .85rem; margin-bottom: 1rem; }
                .summary.failed { border-color: var(--danger); }
                .summary-line { display: flex; align-items: center; gap: .55rem; }
                .status-dot { width: .7rem; height: .7rem; border-radius: 50%; background: var(--success); }
                .failed .status-dot { background: var(--danger); }
                .stats { display: grid; grid-template-columns: repeat(3, 1fr); gap: .45rem; margin-top: .8rem; }
                .stat { padding: .45rem; background: var(--raised); }
                .stat strong { display: block; font-size: 1.1rem; }
                .stat span { color: var(--muted); font-size: .78rem; }
                .links { display: flex; flex-wrap: wrap; gap: .45rem .75rem; margin: .8rem 0 1rem; font-size: .85rem; }
                .frame-nav { list-style: none; padding: 0; margin: 0; }
                .frame-nav li { margin: .3rem 0; }
                .frame-nav a { display: block; padding: .55rem .65rem; color: var(--text); text-decoration: none; border-left: 3px solid transparent; }
                .frame-nav a:hover, .frame-nav a[aria-current="true"] { border-left-color: var(--accent); background: var(--raised); }
                .nav-meta { display: block; margin-top: .15rem; color: var(--muted); font-size: .78rem; }
                .badge { display: inline-block; padding: .08rem .4rem; border: 1px solid currentColor; border-radius: 999px; font-size: .72rem; white-space: nowrap; }
                .badge.passed, .badge.captured, .badge.negotiated, .badge.evidence-captured { color: var(--success); }
                .badge.skipped, .badge.single-monitor, .badge.no-events { color: var(--warning); }
                .badge.failed { color: var(--danger); }
                .badge.neutral { color: var(--muted); }
                .frame { display: none; }
                .frame.is-current { display: block; }
                .frame-heading { display: flex; flex-wrap: wrap; align-items: baseline; gap: .6rem; margin-bottom: .35rem; }
                .narration { margin: 0 0 .8rem; color: var(--muted); }
                .frame-layout { display: grid; grid-template-columns: minmax(0, 1fr) minmax(17rem, 25rem); gap: .8rem; }
                .viewport { min-height: min(62vh, 42rem); display: flex; align-items: center; justify-content: center; padding: .5rem; background: #02070a; border: 1px solid var(--border); }
                .viewport img { display: block; max-width: 100%; max-height: 62vh; object-fit: contain; }
                .viewport.empty { color: var(--muted); }
                .card { padding: .8rem; }
                .card h3 { margin-bottom: .55rem; }
                .event-list { list-style: none; padding: 0; margin: 0; }
                .event-item { padding: .55rem 0; border-top: 1px solid var(--border); }
                .event-item:first-child { padding-top: 0; border-top: 0; }
                .event-line { display: flex; flex-wrap: wrap; align-items: center; gap: .4rem; }
                .event-time { margin-left: auto; color: var(--muted); font-size: .78rem; }
                .detail { margin: .35rem 0 0; color: var(--muted); white-space: pre-wrap; overflow-wrap: anywhere; }
                pre { max-height: 22rem; overflow: auto; margin: .55rem 0 0; padding: .65rem; color: #d8f3f0; background: #061016; border: 1px solid var(--border); font: .78rem/1.45 ui-monospace, Consolas, monospace; white-space: pre-wrap; overflow-wrap: anywhere; }
                details { margin-top: .8rem; padding: .7rem .8rem; }
                summary { cursor: pointer; font-weight: 650; }
                .evidence-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: .8rem; margin-top: .8rem; }
                .controls { display: flex; align-items: center; flex-wrap: wrap; gap: .5rem; margin: .8rem 0; }
                .progress { margin-left: auto; color: var(--muted); }
                .raw video { display: block; width: min(100%, 60rem); max-height: 32rem; margin-top: .65rem; background: #000; }
                @media (max-width: 1050px) {
                  .shell { grid-template-columns: 1fr; }
                  aside { border-right: 0; border-bottom: 1px solid var(--border); }
                  .frame-nav { display: grid; grid-template-columns: repeat(auto-fit, minmax(13rem, 1fr)); gap: .25rem; }
                  .frame-layout, .evidence-grid { grid-template-columns: 1fr; }
                }
              </style>
            </head>
            <body>
              <div class="shell">
                <aside>
                  <p class="eyebrow">Evidence timeline</p>
                  <h1>__TITLE__</h1>
                  <div class="summary __STATUS_CLASS__">
                    <div class="summary-line"><span class="status-dot" aria-hidden="true"></span><strong>__STATUS__</strong></div>
                    <p class="meta">__SECTION_COUNT__ sections · __STEP_COUNT__ captured frames · __EVENT_COUNT__ recorded events</p>
                    <div class="stats">
                      <div class="stat"><strong>__RECORDED__</strong><span>passed/captured</span></div>
                      <div class="stat"><strong>__SKIPPED__</strong><span>skipped</span></div>
                      <div class="stat"><strong>__FAILED__</strong><span>failed</span></div>
                    </div>
                  </div>
                  <nav aria-label="Captured frames">
                    <p class="eyebrow">Frames</p>
                    <ol class="frame-nav">__NAV__</ol>
                  </nav>
                  <div class="links">
                    __INDEX__
                    <a href="timeline.json">timeline.json</a>
                    <a href="timeline.md">timeline.md</a>
                    <a href="logs/events.ndjson">events.ndjson</a>
                    <a href="logs/__LOG_FILE__">__LOG_FILE__</a>
                    __TRACE__
                  </div>
                </aside>
                <main>
                  <p class="eyebrow">Frame review</p>
                  <p class="lede">Select a frame to inspect the image and the events recorded at that state. Telemetry and logs remain visible below the player.</p>
                  __BASE_URL__
                  <section id="frames">__FRAMES__</section>
                  <div class="controls">
                    <button type="button" id="prev">Previous</button>
                    <button type="button" id="play" aria-pressed="false">Play timeline</button>
                    <button type="button" id="next">Next</button>
                    <span class="progress" id="progress"></span>
                  </div>
                  <section class="evidence-grid">
                    <details open><summary>All events (__EVENT_COUNT__)</summary><ol class="event-list">__EVENTS__</ol></details>
                    <details open><summary>Telemetry (__TELEMETRY_COUNT__ records)</summary>__TELEMETRY__</details>
                  </section>
                  <details open><summary>Logs (__LOG_COUNT__ lines)</summary><pre>__LOGS__</pre></details>
                  __RAW__
                </main>
              </div>
              <script>
                function fileHref(href) {
                  try { return new URL(href, location.href).href.replace(/^(file:\/\/\/[A-Za-z])%3A/i, "$1:"); }
                  catch { return href; }
                }
                document.querySelectorAll(".links a").forEach((link) => link.setAttribute("href", fileHref(link.getAttribute("href"))));
                const frames = Array.from(document.querySelectorAll(".frame"));
                const links = Array.from(document.querySelectorAll(".frame-nav a"));
                const progress = document.getElementById("progress");
                const play = document.getElementById("play");
                let index = 0;
                let timer = 0;
                let running = false;
                function show(nextIndex) {
                  if (!frames.length) return;
                  index = Math.max(0, Math.min(nextIndex, frames.length - 1));
                  frames.forEach((frame, i) => frame.classList.toggle("is-current", i === index));
                  links.forEach((link, i) => link.setAttribute("aria-current", i === index ? "true" : "false"));
                  progress.textContent = "Frame " + (index + 1) + " of " + frames.length;
                  const hash = "#frame-" + (index + 1);
                  if (location.hash !== hash) history.replaceState(null, "", hash);
                }
                function setRunning(value) {
                  running = value && frames.length > 1 && index < frames.length - 1;
                  play.textContent = running ? "Pause" : "Play timeline";
                  play.setAttribute("aria-pressed", running ? "true" : "false");
                  window.clearInterval(timer);
                  if (running) timer = window.setInterval(() => {
                    if (index + 1 >= frames.length) { setRunning(false); return; }
                    show(index + 1);
                  }, __HOLD__);
                }
                document.getElementById("prev").addEventListener("click", () => { show(index - 1); setRunning(false); });
                document.getElementById("next").addEventListener("click", () => { show(index + 1); setRunning(false); });
                play.addEventListener("click", () => setRunning(!running));
                links.forEach((link, i) => link.addEventListener("click", (event) => { event.preventDefault(); show(i); setRunning(false); }));
                document.addEventListener("keydown", (event) => {
                  if (event.key === "ArrowRight") { show(index + 1); setRunning(false); }
                  if (event.key === "ArrowLeft") { show(index - 1); setRunning(false); }
                  if (event.key === " ") { event.preventDefault(); setRunning(!running); }
                });
                const hashIndex = Number.parseInt((location.hash.match(/^#frame-(\d+)$/) || [])[1] || "1", 10) - 1;
                show(Number.isFinite(hashIndex) ? hashIndex : 0);
              </script>
            </body>
            </html>
            """;

        var hold = manifest.FrameHoldMilliseconds > 0
            ? manifest.FrameHoldMilliseconds
            : 4000;
        return template
            .Replace("__TITLE__", Encode(TimelineTitle.Display(manifest.Title)), StringComparison.Ordinal)
            .Replace("__INDEX__", indexLink, StringComparison.Ordinal)
            .Replace("__TRACE__", traceLink, StringComparison.Ordinal)
            .Replace("__BASE_URL__", baseUrl, StringComparison.Ordinal)
            .Replace("__RAW__", rawCapture, StringComparison.Ordinal)
            .Replace("__STATUS_CLASS__", StatusClass(runStatus), StringComparison.Ordinal)
            .Replace("__STATUS__", Encode(runStatus), StringComparison.Ordinal)
            .Replace(
                "__SECTION_COUNT__",
                TimelineOutline.SectionCount(manifest.Steps)
                    .ToString(CultureInfo.InvariantCulture),
                StringComparison.Ordinal)
            .Replace("__STEP_COUNT__", manifest.Steps.Count.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("__EVENT_COUNT__", events.Count.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("__RECORDED__", recorded.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("__SKIPPED__", skipped.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("__FAILED__", failed.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("__NAV__", BuildNavigation(manifest, events), StringComparison.Ordinal)
            .Replace("__FRAMES__", BuildFrames(manifest, events, resolveAsset), StringComparison.Ordinal)
            .Replace("__EVENTS__", BuildEvents(events), StringComparison.Ordinal)
            .Replace("__TELEMETRY_COUNT__", telemetry.Count.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("__TELEMETRY__", BuildTelemetry(telemetry), StringComparison.Ordinal)
            .Replace("__LOG_COUNT__", logs.Count.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("__LOGS__", Encode(string.Join(Environment.NewLine, logs)), StringComparison.Ordinal)
            .Replace("__LOG_FILE__", Encode(manifest.LogFileName), StringComparison.Ordinal)
            .Replace("__HOLD__", hold.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    private static string BuildNavigation(
        TimelineManifest manifest,
        IReadOnlyList<TimelineEvent> events)
    {
        var text = new StringBuilder();
        foreach (var step in manifest.Steps)
        {
            var status = events.LastOrDefault(item =>
                string.Equals(item.Name, step.Name, StringComparison.OrdinalIgnoreCase))?.Status
                ?? "captured";
            text.Append("<li><a href=\"#frame-")
                .Append(step.Index.ToString(CultureInfo.InvariantCulture))
                .Append("\">")
                .Append(Encode($"{step.Index}. {step.Name}"))
                .Append("<span class=\"nav-meta\"><span class=\"badge ")
                .Append(StatusClass(status))
                .Append("\">")
                .Append(Encode(status))
                .Append("</span> · ")
                .Append(Encode(FormatTime(step.CapturedAtUtc)));
            if (!string.IsNullOrWhiteSpace(step.FlowPath))
            {
                text.Append(" · ")
                    .Append(Encode(step.FlowPath));
            }

            text
                .Append("</span></a></li>");
        }

        return text.ToString();
    }

    private static string BuildFrames(
        TimelineManifest manifest,
        IReadOnlyList<TimelineEvent> events,
        Func<string, string> resolveAsset)
    {
        var text = new StringBuilder();
        var first = true;
        foreach (var step in manifest.Steps)
        {
            var matchingEvents = events
                .Where(item => string.Equals(
                    item.Name,
                    step.Name,
                    StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var status = matchingEvents.LastOrDefault()?.Status ?? "captured";
            text.Append("<article class=\"frame");
            if (first)
            {
                text.Append(" is-current");
                first = false;
            }

            text.Append("\" id=\"frame-")
                .Append(step.Index.ToString(CultureInfo.InvariantCulture))
                .Append("\"><div class=\"frame-heading\"><h2>")
                .Append(Encode($"{step.Index}. {step.Name}"))
                .Append("</h2>");
            if (!string.IsNullOrWhiteSpace(step.Kind))
            {
                text.Append("<span class=\"badge ")
                    .Append(StatusClass(step.Kind))
                    .Append("\">")
                    .Append(Encode(step.Kind))
                    .Append("</span>");
            }

            text.Append("<span class=\"badge ")
                .Append(StatusClass(status))
                .Append("\">")
                .Append(Encode(status))
                .Append("</span></div><p class=\"narration\">")
                .Append(Encode(step.Narration))
                .Append("</p><div class=\"frame-layout\">");
            if (!string.IsNullOrWhiteSpace(step.FrameFile))
            {
                text.Append("<div class=\"viewport\"><img src=\"")
                    .Append(Encode(resolveAsset(step.FrameFile)))
                    .Append("\" alt=\"")
                    .Append(Encode(step.Name))
                    .Append("\"/></div>");
            }
            else
            {
                text.Append("<div class=\"viewport empty\">No frame was captured.</div>");
            }

            text.Append("<div class=\"card\"><h3>Captured evidence</h3><p class=\"meta\">")
                .Append(Encode(FormatTime(step.CapturedAtUtc)))
                .Append("</p>");
            if (matchingEvents.Length == 0)
            {
                text.Append("<p class=\"detail\">No matching event was recorded for this frame.</p>");
            }
            else
            {
                text.Append("<ol class=\"event-list\">");
                foreach (var item in matchingEvents)
                {
                    text.Append("<li class=\"event-item\">");
                    AppendEvent(text, item);
                    text.Append("</li>");
                }

                text.Append("</ol>");
            }

            text.Append("</div></div></article>");
        }

        return text.Length == 0
            ? "<p class=\"muted\">No frames were captured.</p>"
            : text.ToString();
    }

    private static string BuildEvents(IReadOnlyList<TimelineEvent> events)
    {
        if (events.Count == 0)
        {
            return "<li class=\"event-item\"><span class=\"muted\">No events were recorded.</span></li>";
        }

        var text = new StringBuilder();
        foreach (var item in events)
        {
            text.Append("<li class=\"event-item\">");
            AppendEvent(text, item);
            text.Append("</li>");
        }

        return text.ToString();
    }

    private static void AppendEvent(StringBuilder text, TimelineEvent item)
    {
        text.Append("<div class=\"event-line\"><strong>")
            .Append(Encode($"{item.Sequence}. {item.Name}"))
            .Append("</strong><span class=\"badge ")
            .Append(StatusClass(item.Status))
            .Append("\">")
            .Append(Encode(item.Status))
            .Append("</span><span class=\"event-time\">")
            .Append(Encode(FormatTime(item.CapturedAtUtc)))
            .Append("</span></div>");
        if (!string.IsNullOrWhiteSpace(item.Detail))
        {
            text.Append("<p class=\"detail\">")
                .Append(Encode(item.Detail))
                .Append("</p>");
        }

        if (item.Metadata is { Count: > 0 })
        {
            text.Append("<pre>")
                .Append(Encode(JsonSerializer.Serialize(item.Metadata, JsonOptions)))
                .Append("</pre>");
        }
    }

    private static string BuildTelemetry(IReadOnlyList<object> telemetry)
    {
        if (telemetry.Count == 0)
        {
            return "<p class=\"muted\">No telemetry was recorded.</p>";
        }

        var text = new StringBuilder();
        foreach (var item in telemetry)
        {
            text.Append("<pre>")
                .Append(Encode(JsonSerializer.Serialize(item, JsonOptions)))
                .Append("</pre>");
        }

        return text.ToString();
    }

    private static string FormatTime(DateTimeOffset value) =>
        value.ToUniversalTime().ToString(
            "yyyy-MM-dd HH:mm:ss.fff 'UTC'",
            CultureInfo.InvariantCulture);

    private static string StatusClass(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return "neutral";
        }

        return status.ToLowerInvariant() switch
        {
            "passed" => "passed",
            "captured" => "captured",
            "negotiated" => "negotiated",
            "evidence-captured" => "evidence-captured",
            "skipped" => "skipped",
            "single-monitor" => "single-monitor",
            "failed" => "failed",
            "no-events" => "no-events",
            _ => "neutral",
        };
    }

    private static bool Is(string? value, string expected) =>
        string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);

    private static string Encode(string? value) =>
        WebUtility.HtmlEncode(value ?? string.Empty);
}
