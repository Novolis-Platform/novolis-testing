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
}
