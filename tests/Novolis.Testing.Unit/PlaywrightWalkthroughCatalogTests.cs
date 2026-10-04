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
        Write(newer, "Game_shop", "Set Saturday as a working day", DateTimeOffset.Parse("2026-10-04T19:00:00Z"), withFrame: true);
        Write(other, "Helen_is_hr_and_admin", "Open Rules", DateTimeOffset.Parse("2026-10-04T19:30:00Z"));

        var entries = PlaywrightWalkthroughCatalog.Refresh(root);
        var index = await File.ReadAllTextAsync(Path.Combine(root, "index.md"));
        var html = await File.ReadAllTextAsync(Path.Combine(root, "index.html"));
        var story = await File.ReadAllTextAsync(Path.Combine(newer, "walkthrough.md"));
        var player = await File.ReadAllTextAsync(Path.Combine(newer, "walkthrough.html"));
        var newerRelative = Path.GetRelativePath(root, newer).Replace('\\', '/');

        await Assert.That(entries).Count().IsEqualTo(2);
        await Assert.That(index).Contains("# Walkthroughs");
        await Assert.That(index).Contains("## Hours Game Month Scenario Tests");
        await Assert.That(index).Contains("[Game shop](");
        await Assert.That(index).Contains("walkthrough.md");
        await Assert.That(index).Contains("[Play](");
        await Assert.That(index).DoesNotContain("20260101T000000000");
        await Assert.That(html).Contains("Game shop");
        await Assert.That(html).Contains($"href=\"{newerRelative}/walkthrough.html\"");
        await Assert.That(html).Contains("Each walkthrough.html is one portable file");
        await Assert.That(html).DoesNotContain("id=\"hoursgamemonthscenariotests-game-shop\"");
        await Assert.That(html).DoesNotContain("data:image/png");
        await Assert.That(html).DoesNotContain("Set Saturday as a working day");
        await Assert.That(player).Contains("Set Saturday as a working day");
        await Assert.That(player).Contains("data:image/png;base64,");
        await Assert.That(player).Contains("Play scenario");
        await Assert.That(story).Contains("Set Saturday as a working day");
        await Assert.That(File.Exists(Path.Combine(newer, "walkthrough.html"))).IsTrue();
    }

    private static void Write(string directory, string title, string stepName, DateTimeOffset capturedAt, bool withFrame = false)
    {
        Directory.CreateDirectory(directory);
        if (withFrame)
        {
            var frames = Path.Combine(directory, "frames");
            Directory.CreateDirectory(frames);
            File.WriteAllBytes(
                Path.Combine(frames, "01.png"),
                [
                    0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
                    0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
                    0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
                    0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
                    0x42, 0x60, 0x82,
                ]);
        }

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
