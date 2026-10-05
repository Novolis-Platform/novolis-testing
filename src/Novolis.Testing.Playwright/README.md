<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-testing/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-testing/) · [Source](https://github.com/Novolis-Platform/novolis-testing)
<!-- novolis-pkg-brand:end -->

# Novolis.Testing.Playwright

Walkthrough storage on top of **[TUnit.Playwright](https://www.nuget.org/packages/TUnit.Playwright)** `PageTest`. TUnit owns the browser lifecycle. This package writes a readable **story** of each test:

- `walkthrough.md` — Markdown story for editors and LLMs (frames sit next to the narration)
- `walkthrough.html` — the same pages as a player: steps on the left, one frame that updates on the right
- `index.md` / `index.html` — collection of the latest recording of each scenario
- `frames/NN-step.png` — one full-page shot per `StepAsync`
- `walkthrough.json` — part + step timeline
- `trace.zip` — Playwright trace (optional, on by default)

Real-time WebM is off by default. Playwright finishes a UI test in seconds, so a `<video>` of that run is unreadable. Set `RecordVideo = true` only if you want the raw capture tucked under “Raw capture” in the player.

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
        Session.BeginPart("1. Sign in");
        await Session.StepAsync("Open door", "The week is behind the door.", page => page.GotoAsync("/"));
        using (Session.BeginSection("Set usual hours"))
        {
            await Session.StepAsync("Open My hours", "The usual clock is a form.", page =>
                page.GetByRole(AriaRole.Link, new() { Name = "My hours" }).ClickAsync());
            await Session.StepAsync("Enter 09:00–18:00", "Start and end, then save.", async page =>
            {
                await page.GetByLabel("Usual start").FillAsync("09:00");
                await page.GetByLabel("Usual end").FillAsync("18:00");
            });
        }

        using (Session.BeginFlow("Thursday · Changed", PlaywrightWalkthroughFlowKind.Deviated))
        {
            using (Session.BeginFlow("I worked different hours", PlaywrightWalkthroughFlowKind.Deviated))
            {
                await Session.StepAsync("Type 10:00–18:00", "Late, same length.", async page =>
                {
                    await page.GetByLabel("Start").FillAsync("10:00");
                    await page.GetByLabel("End").FillAsync("18:00");
                });
            }
        }
    }
}
```

After the test, open `walkthrough.md` to read, or `index.html` at the artifact root to jump between scenarios.

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
