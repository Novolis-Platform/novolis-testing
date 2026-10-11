using Novolis.Testing.Timeline;

namespace Novolis.Testing.Unit;

public sealed class TimelineRecorderTests
{
    [Test]
    public async Task Recorder_writes_frames_events_telemetry_and_logs()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "novolis-testing-timeline",
            Guid.NewGuid().ToString("N"));
        var recorder = TimelineRecorder.Create(
            root,
            "Shared evidence timeline",
            "shared.log");

        try
        {
            await recorder.CaptureAsync(
                    "launch",
                    "The test subject launched.",
                    path => File.WriteAllTextAsync(
                        path,
                        "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"16\" height=\"16\"><rect width=\"16\" height=\"16\" fill=\"#6fe4df\"/></svg>"),
                    frameExtension: "svg")
                .ConfigureAwait(false);
            recorder.Record("launch", "passed", "The launch state is visible.");
            recorder.RecordTelemetry("startup-ms", new { value = 128 });
            recorder.Log("Shared recorder completed.");
            await recorder.FlushAsync().ConfigureAwait(false);
            var catalog = TimelineCatalog.Refresh(root, pruneOlderRecordings: false);

            await Assert.That(recorder.Events.Count).IsEqualTo(2);
            await Assert.That(catalog).Count().IsEqualTo(1);
            await Assert.That(recorder.Events[0].Sequence).IsEqualTo(1);
            await Assert.That(recorder.Events[1].Sequence).IsEqualTo(2);
            await Assert.That(
                    File.Exists(Path.Combine(
                        recorder.ArtifactDirectory,
                        "frames",
                        "01-launch.svg")))
                .IsTrue();
            var html = await File.ReadAllTextAsync(
                    Path.Combine(recorder.ArtifactDirectory, "timeline.html"))
                .ConfigureAwait(false);
            await Assert.That(html).Contains("Shared evidence timeline");
            await Assert.That(html).Contains("All events (2)");
            await Assert.That(html).Contains("startup-ms");
            await Assert.That(html).Contains("data:image/svg+xml;base64,");
            await Assert.That(html).Contains("shared.log");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
