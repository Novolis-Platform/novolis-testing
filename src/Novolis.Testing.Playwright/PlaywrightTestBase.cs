using Microsoft.Playwright;
using TUnit.Core;
using TUnit.Playwright;

namespace Novolis.Testing.Playwright;

/// <summary>
/// <see cref="PageTest"/> plus per-test walkthrough storage. Inherit as <c>MyTests : PlaywrightTestBase</c>.
/// TUnit owns the browser; this type writes step frames and an HTML recording.
/// </summary>
public abstract class PlaywrightTestBase : PageTest
{
    private PlaywrightSessionOptions options = new();
    private string artifacts = string.Empty;

    /// <summary>Hands Chromium launch to TUnit. <c>NOVOLIS_PLAYWRIGHT_HEADED=1</c> shows the window.</summary>
    protected PlaywrightTestBase()
        : base(new BrowserTypeLaunchOptions
        {
            Headless = new PlaywrightSessionOptions().ResolveHeadless(),
        })
    {
    }

    /// <summary>Walkthrough attached after TUnit creates <see cref="PageTest.Page"/>.</summary>
    protected PlaywrightSession Session { get; private set; } = null!;

    /// <summary>Builds launch options for this test. Override to set <see cref="PlaywrightSessionOptions.BaseUrl"/>.</summary>
    /// <returns>Session options.</returns>
    protected virtual PlaywrightSessionOptions CreateOptions() => new()
    {
        ArtifactDirectory = PlaywrightArtifactStore.ForCurrentTest(),
        WalkthroughTitle = TestContext.Current?.Metadata.TestName ?? GetType().Name,
    };

    /// <inheritdoc />
    public override BrowserNewContextOptions ContextOptions(TestContext testContext)
    {
        options = CreateOptions();
        artifacts = string.IsNullOrWhiteSpace(options.ArtifactDirectory)
            ? PlaywrightArtifactStore.ForCurrentTest()
            : options.ArtifactDirectory;
        Directory.CreateDirectory(artifacts);
        Directory.CreateDirectory(Path.Combine(artifacts, "frames"));
        return new BrowserNewContextOptions
        {
            BaseURL = options.BaseUrl?.AbsoluteUri,
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize
            {
                Width = options.ViewportWidth,
                Height = options.ViewportHeight,
            },
            RecordVideoDir = options.RecordVideo ? artifacts : null,
            RecordVideoSize = options.RecordVideo
                ? new RecordVideoSize
                {
                    Width = options.ViewportWidth,
                    Height = options.ViewportHeight,
                }
                : null,
        };
    }

    /// <summary>Ensures Chromium is present and starts an optional trace.</summary>
    /// <returns>A task that completes when the walkthrough is attached.</returns>
    [Before(HookType.Test, Order = 10)]
    public async Task AttachWalkthroughAsync()
    {
        if (!PlaywrightBrowserInstaller.EnsureChromium())
        {
            Skip.Test(
                "Playwright Chromium is not installed. TUnit.Playwright launches the browser after Microsoft.Playwright.Program.Main([\"install\", \"chromium\"]).");
        }

        SetDefaultExpectTimeout(90_000);
        Session = new PlaywrightSession(Page, options with { ArtifactDirectory = artifacts });
        if (options.RecordTrace)
        {
            await Context.Tracing.StartAsync(new TracingStartOptions
            {
                Screenshots = true,
                Snapshots = true,
                Sources = true,
            });
        }
    }

    /// <summary>Stops the Playwright trace while the TUnit context is still open.</summary>
    /// <returns>A task that completes when the trace zip is written.</returns>
    [After(HookType.Test, Order = -10)]
    public async Task StopTraceAsync()
    {
        if (options.RecordTrace && Context is not null)
        {
            await Context.Tracing.StopAsync(new TracingStopOptions
            {
                Path = Path.Combine(artifacts, "trace.zip"),
            });
        }
    }

    /// <summary>Writes frames and the HTML player. Video may still be finalizing.</summary>
    /// <returns>A task that completes when artifacts are flushed.</returns>
    [After(HookType.Test, Order = 10)]
    public async Task FlushWalkthroughAsync()
    {
        if (Session is not null)
        {
            await Session.FlushAsync();
        }
    }
}
