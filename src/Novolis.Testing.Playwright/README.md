<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-testing/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-testing/) · [Source](https://github.com/Novolis-Platform/novolis-testing)
<!-- novolis-pkg-brand:end -->

# Novolis.Testing.Playwright

Walkthrough storage on top of **[TUnit.Playwright](https://www.nuget.org/packages/TUnit.Playwright)** `PageTest`. TUnit owns the browser lifecycle. This package writes a watchable record of each test:

- `{TestName}.webm` — TUnit.Playwright screen recording (`RecordVideoDir` on the context; copied to `video.webm` when the file is already finalized)
- `frames/NN-step.png` — one full-page shot per `StepAsync`
- `walkthrough.json` — step timeline
- `walkthrough.html` — local player for the video and frames
- `trace.zip` — Playwright trace (optional, on by default)

This package does not start the application under test. Hours (and other hosts) start their own API and Blazor surfaces, then pass `BaseUrl`.

## Install

```bash
dotnet add package Novolis.Testing.Playwright
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), [TUnit.Playwright](https://www.nuget.org/packages/TUnit.Playwright). Chromium is installed through `Microsoft.Playwright.Program.Main(["install", "chromium"])` — C#, not Node. Override the artifact root with `NOVOLIS_PLAYWRIGHT_ARTIFACTS`. Set `NOVOLIS_PLAYWRIGHT_HEADED=1` to show the browser.

## Quick start

```csharp
using Microsoft.Playwright;
using Novolis.Testing.Playwright;
using TUnit.Core;

public sealed class DoorTests : PlaywrightTestBase
{
    protected override PlaywrightSessionOptions CreateOptions() => new()
    {
        BaseUrl = new Uri("http://localhost:5731/"),
        ArtifactDirectory = PlaywrightArtifactStore.ForCurrentTest(),
        WalkthroughTitle = "Hours door",
    };

    [Test]
    public async Task Sign_in_reaches_the_week()
    {
        await Session.StepAsync("Open door", page => page.GotoAsync("/"));
        await Session.StepAsync("Enter", async page =>
        {
            await page.GetByLabel("Login").FillAsync("ada");
            await page.GetByRole(AriaRole.Button, new() { Name = "Enter this week" }).ClickAsync();
        });
    }
}
```

After the test, open `walkthrough.html` in the artifact folder.

## Related packages

| Package | When to use |
|---------|-------------|
| `TUnit.Playwright` | Browser lifecycle (`PageTest`, `[RecordVideo]`) — this package sits on it |
| `Novolis.Testing.Appium` | Android / Windows MAUI hosts |
| `Novolis.Testing.TUnit` | JSON/table dump helpers for assertions |
| `Novolis.Testing.TestServer` | In-process HTTP (not a real browser) |

## More documentation

- [Getting started](https://github.com/Novolis-Platform/novolis-testing/blob/main/docs/getting-started.md)
- [Design](https://github.com/Novolis-Platform/novolis-testing/blob/main/docs/design.md)

## Support

Pre-release (`2026.1.*` on GitHub Packages).
