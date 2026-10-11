using Novolis.Testing.Appium;

namespace Novolis.Testing.Unit;

public sealed class AppiumArtifactStoreTests
{
    [Test]
    public async Task Sanitize_replaces_invalid_file_name_characters()
    {
        await Assert.That(AppiumArtifactStore.Sanitize(@"Admin: setup / Game"))
            .IsEqualTo("Admin__setup___Game");
    }

    [Test]
    [NotInParallel("appium-artifact-root")]
    public async Task ResolveRoot_prefers_explicit_then_environment()
    {
        var previous = Environment.GetEnvironmentVariable(AppiumArtifactStore.RootEnvironmentVariable);
        var explicitRoot = Path.Combine(Path.GetTempPath(), "novolis-appium-explicit");
        var envRoot = Path.Combine(Path.GetTempPath(), "novolis-appium-env");
        try
        {
            Environment.SetEnvironmentVariable(AppiumArtifactStore.RootEnvironmentVariable, envRoot);
            await Assert.That(AppiumArtifactStore.ResolveRoot(explicitRoot))
                .IsEqualTo(Path.GetFullPath(explicitRoot));
            await Assert.That(AppiumArtifactStore.ResolveRoot())
                .IsEqualTo(Path.GetFullPath(envRoot));
        }
        finally
        {
            Environment.SetEnvironmentVariable(AppiumArtifactStore.RootEnvironmentVariable, previous);
        }
    }

    [Test]
    public async Task ForCurrentTest_creates_frames_folder()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-appium-tests", Guid.NewGuid().ToString("N"));
        var directory = AppiumArtifactStore.ForCurrentTest("artifact_store", root);
        await Assert.That(Directory.Exists(Path.Combine(directory, "frames"))).IsTrue();
        await Assert.That(directory.Contains("artifact_store", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task ForCurrentTest_clears_previous_stamps_for_the_same_identity()
    {
        var root = Path.Combine(Path.GetTempPath(), "novolis-appium-clean", Guid.NewGuid().ToString("N"));
        var first = AppiumArtifactStore.ForCurrentTest("same_identity", root);
        await File.WriteAllTextAsync(Path.Combine(first, "old.txt"), "gone");
        var other = AppiumArtifactStore.ForCurrentTest("other_identity", root);
        await File.WriteAllTextAsync(Path.Combine(other, "keep.txt"), "stay");

        var second = AppiumArtifactStore.ForCurrentTest("same_identity", root);

        await Assert.That(File.Exists(Path.Combine(first, "old.txt"))).IsFalse();
        await Assert.That(Directory.Exists(Path.Combine(second, "frames"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(other, "keep.txt"))).IsTrue();
    }

    [Test]
    public async Task CatalogRootFrom_walks_up_to_the_appium_folder()
    {
        var stamp = Path.Combine(
            Path.GetTempPath(),
            "appium",
            "ReachAndroidTimelineTests",
            "AndroidClientProducesLaunchEvidence",
            "20261011T010000000");
        await Assert.That(AppiumArtifactStore.CatalogRootFrom(stamp))
            .IsEqualTo(Path.GetFullPath(Path.Combine(Path.GetTempPath(), "appium")));
    }
}
