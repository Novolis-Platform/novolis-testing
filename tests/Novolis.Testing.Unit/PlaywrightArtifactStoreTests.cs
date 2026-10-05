using Novolis.Testing.Playwright;

namespace Novolis.Testing.Unit;

public sealed class PlaywrightArtifactStoreTests
{
    [Test]
    public async Task Sanitize_replaces_invalid_file_name_characters()
    {
        await Assert.That(PlaywrightArtifactStore.Sanitize(@"Admin: setup / Game"))
            .IsEqualTo("Admin__setup___Game");
    }

    [Test]
    [NotInParallel]
    public async Task ResolveRoot_prefers_explicit_then_environment()
    {
        var previous = Environment.GetEnvironmentVariable(PlaywrightArtifactStore.RootEnvironmentVariable);
        var explicitRoot = Path.Combine(Path.GetTempPath(), "novolis-playwright-explicit");
        var envRoot = Path.Combine(Path.GetTempPath(), "novolis-playwright-env");
        try
        {
            Environment.SetEnvironmentVariable(PlaywrightArtifactStore.RootEnvironmentVariable, envRoot);
            await Assert.That(PlaywrightArtifactStore.ResolveRoot(explicitRoot))
                .IsEqualTo(Path.GetFullPath(explicitRoot));
            await Assert.That(PlaywrightArtifactStore.ResolveRoot())
                .IsEqualTo(Path.GetFullPath(envRoot));
        }
        finally
        {
            Environment.SetEnvironmentVariable(PlaywrightArtifactStore.RootEnvironmentVariable, previous);
        }
    }

    [Test]
    public async Task ForCurrentTest_creates_frames_folder()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-playwright-tests", Guid.NewGuid().ToString("N"));
        var directory = PlaywrightArtifactStore.ForCurrentTest("artifact_store", root);
        await Assert.That(Directory.Exists(Path.Combine(directory, "frames"))).IsTrue();
        await Assert.That(directory.Contains("artifact_store", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task ForCurrentTest_clears_previous_stamps_for_the_same_identity()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-playwright-clean", Guid.NewGuid().ToString("N"));
        var first = PlaywrightArtifactStore.ForCurrentTest("same_identity", root);
        await File.WriteAllTextAsync(Path.Combine(first, "old.txt"), "gone");
        var other = PlaywrightArtifactStore.ForCurrentTest("other_identity", root);
        await File.WriteAllTextAsync(Path.Combine(other, "keep.txt"), "stay");

        var second = PlaywrightArtifactStore.ForCurrentTest("same_identity", root);

        await Assert.That(File.Exists(Path.Combine(first, "old.txt"))).IsFalse();
        await Assert.That(Directory.Exists(Path.Combine(second, "frames"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(other, "keep.txt"))).IsTrue();
    }

    [Test]
    public async Task CatalogRootFrom_walks_up_to_the_playwright_folder()
    {
        var stamp = Path.Combine(
            Path.GetTempPath(),
            "playwright",
            "HoursMixedRoleTests",
            "Alice_has_a_week",
            "20261004T193127887");
        await Assert.That(PlaywrightArtifactStore.CatalogRootFrom(stamp))
            .IsEqualTo(Path.GetFullPath(Path.Combine(Path.GetTempPath(), "playwright")));
    }
}
