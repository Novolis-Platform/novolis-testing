using System.Text.Json;
using Microsoft.Playwright;

namespace Novolis.Testing.Playwright;

/// <summary>
/// Walkthrough recorder for a page owned by <c>TUnit.Playwright.PageTest</c>.
/// Does not launch Chromium — TUnit does that.
/// </summary>
public sealed class PlaywrightSession
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly List<PlaywrightWalkthroughStep> steps = [];
    private readonly PlaywrightSessionOptions options;

    /// <summary>Attaches to a TUnit-owned page.</summary>
    /// <param name="page">Page from <c>PageTest.Page</c>.</param>
    /// <param name="options">Walkthrough and artifact options.</param>
    public PlaywrightSession(IPage page, PlaywrightSessionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(page);
        Page = page;
        this.options = options ?? new PlaywrightSessionOptions();
        ArtifactDirectory = string.IsNullOrWhiteSpace(this.options.ArtifactDirectory)
            ? PlaywrightArtifactStore.ForCurrentTest()
            : this.options.ArtifactDirectory;
        Directory.CreateDirectory(ArtifactDirectory);
        Directory.CreateDirectory(Path.Combine(ArtifactDirectory, "frames"));
    }

    /// <summary>Active page for the test.</summary>
    public IPage Page { get; }

    /// <summary>Folder that receives video, frames, and the walkthrough manifest.</summary>
    public string ArtifactDirectory { get; }

    /// <summary>Absolute walkthrough JSON path after <see cref="FlushAsync"/>.</summary>
    public string ManifestPath => Path.Combine(ArtifactDirectory, "walkthrough.json");

    /// <summary>Absolute HTML player path after <see cref="FlushAsync"/>.</summary>
    public string PlayerPath => Path.Combine(ArtifactDirectory, "walkthrough.html");

    /// <summary>Recorded steps captured so far.</summary>
    public IReadOnlyList<PlaywrightWalkthroughStep> Steps => steps;

    /// <summary>
    /// Runs one named walkthrough step, then stores a PNG frame so the HTML player can replay it.
    /// </summary>
    /// <param name="name">Human-readable step title.</param>
    /// <param name="action">Work performed on <see cref="Page"/>.</param>
    /// <returns>A task that completes when the frame is written.</returns>
    public async Task StepAsync(string name, Func<IPage, Task> action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(action);
        await action(Page);
        await CaptureAsync(name);
    }

    /// <summary>Stores a screenshot frame without performing extra page work.</summary>
    /// <param name="name">Human-readable step title.</param>
    /// <returns>A task that completes when the frame is written.</returns>
    public async Task CaptureAsync(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var index = steps.Count + 1;
        var fileName = $"{index:00}-{PlaywrightArtifactStore.Sanitize(name)}.png";
        var path = Path.Combine(ArtifactDirectory, "frames", fileName);
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = path,
            FullPage = true,
        });
        steps.Add(new PlaywrightWalkthroughStep(index, name, $"frames/{fileName}", DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Writes the walkthrough JSON and HTML player. Copies <c>video.webm</c> when Playwright has one.
    /// Does not close the TUnit browser context.
    /// </summary>
    /// <returns>A task that completes when artifacts are flushed.</returns>
    public async Task FlushAsync()
    {
        string? videoFile = await CopyVideoAsync();

        var traceFile = File.Exists(Path.Combine(ArtifactDirectory, "trace.zip"))
            ? "trace.zip"
            : null;
        var manifest = new PlaywrightWalkthroughManifest(
            options.WalkthroughTitle,
            options.BaseUrl?.AbsoluteUri,
            ArtifactDirectory,
            videoFile,
            traceFile,
            steps);
        await File.WriteAllTextAsync(ManifestPath, JsonSerializer.Serialize(manifest, JsonOptions));
        await File.WriteAllTextAsync(PlayerPath, RenderPlayer(manifest));
    }

    private async Task<string?> CopyVideoAsync()
    {
        var destination = Path.Combine(ArtifactDirectory, "video.webm");
        string? candidate = null;
        try
        {
            var recorded = Page.Video is null ? null : await Page.Video.PathAsync();
            if (IsUsableVideo(recorded))
            {
                candidate = recorded;
            }
        }
        catch (PlaywrightException)
        {
        }

        if (candidate is null && Directory.Exists(ArtifactDirectory))
        {
            candidate = Directory.GetFiles(ArtifactDirectory, "*.webm")
                .Where(IsUsableVideo)
                .OrderByDescending(path => new FileInfo(path).Length)
                .FirstOrDefault();
        }

        if (candidate is null)
        {
            if (!options.RecordVideo)
            {
                return null;
            }

            // TUnit.Playwright writes {TestName}.webm after it closes the context.
            return PlaywrightArtifactStore.Sanitize(options.WalkthroughTitle) + ".webm";
        }

        if (!string.Equals(candidate, destination, StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(candidate, destination, overwrite: true);
        }

        return IsUsableVideo(destination) ? "video.webm" : Path.GetFileName(candidate);
    }

    private static bool IsUsableVideo(string? path) =>
        !string.IsNullOrWhiteSpace(path) && File.Exists(path) && new FileInfo(path).Length > 0;

    private static string RenderPlayer(PlaywrightWalkthroughManifest manifest)
    {
        var frames = string.Join(
            Environment.NewLine,
            manifest.Steps.Select(step =>
                $"<figure><img src=\"{step.FrameFile}\" alt=\"{step.Name}\"/><figcaption>{step.Index}. {step.Name}</figcaption></figure>"));
        var video = string.IsNullOrWhiteSpace(manifest.VideoFile)
            ? string.Empty
            : $"<video controls autoplay muted src=\"{manifest.VideoFile}\"></video>";
        return
            $$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8"/>
              <title>{{manifest.Title}}</title>
              <style>
                body { font-family: Segoe UI, sans-serif; margin: 1.5rem; background: #111; color: #eee; }
                video { width: min(100%, 1440px); display: block; margin-bottom: 1.5rem; }
                figure { margin: 0 0 1rem; }
                img { width: min(100%, 1440px); border: 1px solid #333; }
                figcaption { margin-top: .4rem; color: #bbb; }
              </style>
            </head>
            <body>
              <h1>{{manifest.Title}}</h1>
              {{video}}
              {{frames}}
            </body>
            </html>
            """;
    }
}
