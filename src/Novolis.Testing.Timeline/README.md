<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-testing/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-testing/) · [Source](https://github.com/Novolis-Platform/novolis-testing)
<!-- novolis-pkg-brand:end -->

# Novolis.Testing.Timeline

Framework-neutral evidence timelines for UI, integration, and system tests.

`TimelineRecorder` accepts frame writers from Appium, Playwright, native UI
drivers, or deterministic fixtures. It writes:

- `timeline.html` — a portable frame review with event, telemetry, and log panels
- `timeline.md` — a readable text summary
- `timeline.json` — structured manifest and evidence
- `frames/` — captured images
- `logs/` — event NDJSON and the recorder log

## Install

```xml
<PackageReference Include="Novolis.Testing.Timeline" Version="2026.1.*" />
```

## Quick start

```csharp
using Novolis.Testing.Timeline;

var timeline = TimelineRecorder.Create(
    @"d:\artifacts\my-run",
    "Reach LAN usability");

await timeline.CaptureAsync(
    "client-launched",
    "The client is ready for a trusted endpoint.",
    path => SaveScreenshotAsync(path));
timeline.Record("connection", "passed", "The first frame arrived.");
timeline.RecordTelemetry("time-to-first-frame", new { milliseconds = 842 });
timeline.Log("Run completed.");
await timeline.FlushAsync();
```

The recorder has no browser, mobile, desktop UI, or test framework dependency. `Novolis.Testing.Appium` and `Novolis.Testing.Playwright` are the driver adapters: each test calls `CaptureAsync` with a screenshot writer.
