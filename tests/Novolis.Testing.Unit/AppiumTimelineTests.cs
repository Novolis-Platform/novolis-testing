using Novolis.Testing.Appium;
using Novolis.Testing.Timeline;

namespace Novolis.Testing.Unit;

public sealed class AppiumTimelineTests
{
    [Test]
    public async Task Capture_writes_a_frame_and_flush_writes_the_story()
    {
        var root = CatalogRoot();
        var directory = Path.Combine(root, "ReachAndroid", "launch");
        var timeline = Open(directory, "Reach Android");

        timeline.BeginPart("1. Launch");
        using (timeline.BeginFlow("Trusted endpoint", TimelineFlowKind.Planned))
        {
            await timeline.CaptureAsync(
                "client-launched",
                "The client is ready for a trusted endpoint.",
                new Dictionary<string, object?> { ["platform"] = "Android" });
        }

        timeline.Record("endpoint", "skipped", "No endpoint configured.");
        timeline.RecordTelemetry("time-to-first-frame", new { milliseconds = 842 });
        timeline.Log("Run completed.");
        await timeline.FlushAsync();

        await Assert.That(timeline.CurrentPart).IsEqualTo("1. Launch");
        await Assert.That(timeline.Steps).Count().IsEqualTo(1);
        await Assert.That(timeline.Steps[0].Part).IsEqualTo("1. Launch");
        await Assert.That(timeline.Steps[0].Flow).IsEqualTo("Trusted endpoint");
        await Assert.That(timeline.Events).Count().IsEqualTo(2);
        await Assert.That(File.Exists(Path.Combine(directory, timeline.Steps[0].FrameFile))).IsTrue();
        var json = await File.ReadAllTextAsync(timeline.TimelinePath);
        await Assert.That(json).Contains("Reach Android");
        await Assert.That(json).Contains("client-launched");
        await Assert.That(json).Contains("time-to-first-frame");
        var html = await File.ReadAllTextAsync(timeline.HtmlPath);
        await Assert.That(html).Contains("The client is ready for a trusted endpoint.");
        var markdown = await File.ReadAllTextAsync(timeline.MarkdownPath);
        await Assert.That(markdown).Contains("Trusted endpoint");
        var log = await File.ReadAllTextAsync(Path.Combine(directory, "logs", "timeline.log"));
        await Assert.That(log).Contains("Run completed.");
        await Assert.That(File.Exists(Path.Combine(root, "index.html"))).IsTrue();
    }

    [Test]
    public async Task Capture_rejects_blank_narration()
    {
        var timeline = Open(Path.Combine(CatalogRoot(), "blank"), "Appium timeline");
        await Assert.That(async () => await timeline.CaptureAsync("client-launched", " "))
            .Throws<ArgumentException>();
    }

    private static AppiumTimeline Open(string directory, string title) =>
        new(
            new AppiumTimelineOptions
            {
                ArtifactDirectory = directory,
                TimelineTitle = title,
            },
            static path =>
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, [0x89, 0x50, 0x4E, 0x47]);
                return Task.CompletedTask;
            });

    private static string CatalogRoot() =>
        Path.Combine(
            Path.GetTempPath(),
            "novolis-appium-timeline",
            Guid.NewGuid().ToString("N"),
            "appium");
}
