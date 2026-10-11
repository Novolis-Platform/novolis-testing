<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-testing/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-testing/) · [Source](https://github.com/Novolis-Platform/novolis-testing)
<!-- novolis-pkg-brand:end -->

# Novolis.Testing.Appium

TUnit helpers that open Android and Windows Appium sessions for MAUI hosts and write a per-test evidence timeline from driver screenshots. The C# client connects to an already running Appium server. This package does not reference `Microsoft.Maui.*` and does not start Appium, install drivers, or launch emulators.

## Install

```bash
dotnet add package Novolis.Testing.Appium
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), [TUnit](https://www.nuget.org/packages/TUnit), [Appium server](https://appium.io/docs/en/latest/quickstart/test-dotnet/) already listening (default `http://127.0.0.1:4723/`). Install UiAutomator2 and/or the Windows driver on that server outside this repo. Override the URI with `APPIUM_HOST`.

## Quick start (Android)

```csharp
using Novolis.Testing.Appium;
using TUnit.Core;

public sealed class SettingsTests : AndroidAppiumTestBase<SettingsTests>
{
    protected override AndroidAppiumSessionOptions CreateOptions() => new()
    {
        AppPackage = "com.android.settings",
        AppActivity = ".Settings",
        NoReset = true,
    };

    [Test]
    public async Task Apps_row_opens()
    {
        await Timeline.StepAsync(
            "Open Settings",
            "The apps list is the first screen.",
            _ => Session.StartActivity("com.android.settings", ".Settings"));
        await Assert.That(Driver).IsNotNull();
    }
}
```

Point `AppPath` at a built APK when the package is not already on the device. `StartActivity` sends `mobile:startActivity` (the 5.x `AndroidDriver.StartActivity(package, activity)` overload is gone).

## Quick start (Windows)

```csharp
using Novolis.Testing.Appium;

public sealed class HostTests : WindowsAppiumTestBase<HostTests>
{
    protected override WindowsAppiumSessionOptions CreateOptions() => new()
    {
        App = @"C:\apps\Merglyph.exe",
    };
}
```

`App` may be an exe path or a packaged application id. The server must run `appium-windows-driver`; this library only sets capabilities.

## Timeline

The driver session stays class-scoped. Each test gets its own recording. `Timeline.StepAsync` runs the driver work, then stores a PNG via `GetScreenshot`. `Timeline.CaptureAsync` stores the current screen without extra work. `Record`, `RecordTelemetry`, and `Log` write evidence that has no frame. The test fixture flushes `timeline.json`, `timeline.md`, and `timeline.html` after the test and refreshes `index.html` at the collection root.

Override the title without losing the stamp folder:

```csharp
protected override AppiumTimelineOptions CreateTimelineOptions() =>
    base.CreateTimelineOptions() with { TimelineTitle = "Reach Android" };
```

Sessions opened with `Connect` can record without the fixture:

```csharp
using var session = AndroidAppiumSession.Connect(new AndroidAppiumSessionOptions
{
    AppPath = apk,
    NoReset = true,
});
var timeline = new AppiumTimeline(session.Driver, new AppiumTimelineOptions
{
    TimelineTitle = "Reach Android",
});
await timeline.CaptureAsync(
    "client-launched",
    "The client is ready for a trusted endpoint.");
await timeline.FlushAsync();
```

Artifact root: `NOVOLIS_APPIUM_ARTIFACTS`, otherwise `artifacts/appium` beside the nearest `artifacts` directory. Frames land in `frames/NN-step.png`.

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Testing.Timeline` | Frame, event, and HTML model this package writes |
| `Novolis.Testing.Playwright` | The same timeline shape on a browser page |
| `Novolis.Testing.TestBases` | In-process host fixtures (not device UI) |
| `Novolis.Testing.TUnit` | JSON/table dump helpers for assertions |

## More documentation

- [Getting started](https://github.com/Novolis-Platform/novolis-testing/blob/main/docs/getting-started.md)
- [Design](https://github.com/Novolis-Platform/novolis-testing/blob/main/docs/design.md)

## Support

Pre-release (`2026.1.*` on GitHub Packages).
