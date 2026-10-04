using Novolis.Testing.Playwright;

namespace Novolis.Testing.Unit;

public sealed class PlaywrightWalkthroughCatalogTests
{
    [Test]
    public async Task Refresh_rewrites_stories_and_indexes_the_latest_of_each_test()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-walkthrough-index", Guid.NewGuid().ToString("N"));
        var older = Path.Combine(root, "HoursGameMonthScenarioTests", "Game_shop", "20260101T000000000");
        var newer = Path.Combine(root, "HoursGameMonthScenarioTests", "Game_shop", "20261004T190000000");
        var other = Path.Combine(root, "HoursMixedRoleTests", "Helen_is_hr_and_admin", "20261004T193000000");
        Write(older, "Game_shop", "Open People", DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        Write(newer, "Game_shop", "Set Saturday as a working day", DateTimeOffset.Parse("2026-10-04T19:00:00Z"));
        Write(other, "Helen_is_hr_and_admin", "Open Rules", DateTimeOffset.Parse("2026-10-04T19:30:00Z"));

        var entries = PlaywrightWalkthroughCatalog.Refresh(root);
        var index = await File.ReadAllTextAsync(Path.Combine(root, "index.md"));
        var html = await File.ReadAllTextAsync(Path.Combine(root, "index.html"));
        var story = await File.ReadAllTextAsync(Path.Combine(newer, "walkthrough.md"));

        await Assert.That(entries).Count().IsEqualTo(2);
        await Assert.That(index).Contains("# Walkthroughs");
        await Assert.That(index).Contains("## Hours Game Month Scenario Tests");
        await Assert.That(index).Contains("[Game shop](");
        await Assert.That(index).Contains("walkthrough.md");
        await Assert.That(index).Contains("[Play](");
        await Assert.That(index).DoesNotContain("20260101T000000000");
        await Assert.That(html).Contains("Game shop");
        await Assert.That(await File.ReadAllTextAsync(Path.Combine(newer, "walkthrough.html")))
            .Contains("Set Saturday as a working day");
        await Assert.That(story).Contains("Set Saturday as a working day");
        await Assert.That(File.Exists(Path.Combine(newer, "walkthrough.html"))).IsTrue();
    }

    private static void Write(string directory, string title, string stepName, DateTimeOffset capturedAt)
    {
        Directory.CreateDirectory(directory);
        var manifest = new PlaywrightWalkthroughManifest(
            title,
            null,
            directory,
            RawCaptureFile: null,
            TraceFile: null,
            FrameHoldMilliseconds: 4000,
            [
                new PlaywrightWalkthroughStep(1, "1. Shop", stepName, "Narration.", "frames/01.png", capturedAt),
            ]);
        File.WriteAllText(
            Path.Combine(directory, "walkthrough.json"),
            System.Text.Json.JsonSerializer.Serialize(manifest, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            }));
    }
}
