using TUnit.Core;

namespace Novolis.Testing.Playwright;

/// <summary>
/// Resolves a writable folder for Playwright videos, screenshot frames, and walkthrough manifests.
/// </summary>
public static class PlaywrightArtifactStore
{
    /// <summary>Environment variable that overrides the artifact root.</summary>
    public const string RootEnvironmentVariable = "NOVOLIS_PLAYWRIGHT_ARTIFACTS";

    /// <summary>
    /// Resolves the artifact root. Preference: explicit path, then
    /// <see cref="RootEnvironmentVariable"/>, then <c>artifacts/playwright</c> under the nearest
    /// directory that already has an <c>artifacts</c> folder, then the test output directory.
    /// </summary>
    /// <param name="explicitRoot">Optional absolute or relative root.</param>
    /// <returns>Absolute artifact root.</returns>
    public static string ResolveRoot(string? explicitRoot = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitRoot))
        {
            return Path.GetFullPath(explicitRoot);
        }

        var fromEnvironment = Environment.GetEnvironmentVariable(RootEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return Path.GetFullPath(fromEnvironment);
        }

        var cursor = new DirectoryInfo(AppContext.BaseDirectory);
        while (cursor is not null)
        {
            var artifacts = Path.Combine(cursor.FullName, "artifacts", "playwright");
            if (Directory.Exists(Path.Combine(cursor.FullName, "artifacts")) ||
                File.Exists(Path.Combine(cursor.FullName, "Directory.Build.props")))
            {
                return artifacts;
            }

            cursor = cursor.Parent;
        }

        return Path.Combine(AppContext.BaseDirectory, "playwright-artifacts");
    }

    /// <summary>
    /// Walks from a per-test stamp folder up to the collection root
    /// (<c>artifacts/playwright</c>), so the index sits next to the recordings.
    /// </summary>
    /// <param name="artifactDirectory">Stamp folder written by <see cref="ForCurrentTest"/>.</param>
    /// <returns>Absolute collection root.</returns>
    public static string CatalogRootFrom(string artifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactDirectory);
        var cursor = new DirectoryInfo(Path.GetFullPath(artifactDirectory));
        while (cursor is not null)
        {
            if (cursor.Name.Equals("playwright", StringComparison.OrdinalIgnoreCase))
            {
                return cursor.FullName;
            }

            cursor = cursor.Parent;
        }

        return ResolveRoot();
    }

    /// <summary>
    /// Builds a unique folder for the current TUnit test, or <paramref name="testName"/> when supplied.
    /// Previous stamp folders for the same class and test name are removed first.
    /// </summary>
    /// <param name="testName">Optional test identity. When omitted, TUnit <see cref="TestContext"/> is used.</param>
    /// <param name="explicitRoot">Optional artifact root override.</param>
    /// <returns>Created directory path.</returns>
    public static string ForCurrentTest(string? testName = null, string? explicitRoot = null)
    {
        var metadata = TestContext.Current?.Metadata;
        var className = Sanitize(metadata?.TestDetails.ClassType.Name ?? "Tests");
        var name = Sanitize(testName ?? metadata?.TestName ?? "anonymous");
        var identity = Path.Combine(ResolveRoot(explicitRoot), className, name);
        CleanIdentity(identity);
        var stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff");
        var directory = Path.Combine(identity, stamp);
        Directory.CreateDirectory(directory);
        Directory.CreateDirectory(Path.Combine(directory, "frames"));
        return directory;
    }

    /// <summary>
    /// Deletes stamp folders under one test identity (<c>{root}/{class}/{test}</c>)
    /// so a re-run keeps a single recording.
    /// </summary>
    /// <param name="identityDirectory">Class and test folder, not the stamp.</param>
    public static void CleanIdentity(string identityDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identityDirectory);
        if (!Directory.Exists(identityDirectory))
        {
            return;
        }

        foreach (var child in Directory.GetDirectories(identityDirectory))
        {
            TryDeleteDirectory(child);
        }

        foreach (var file in Directory.GetFiles(identityDirectory))
        {
            try
            {
                File.Delete(file);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    /// <summary>
    /// Keeps the latest completed recording per test and deletes older stamp folders.
    /// In-progress folders without <c>walkthrough.json</c> are left alone.
    /// </summary>
    /// <param name="root">Playwright artifact root.</param>
    /// <param name="keep">Absolute stamp folders that must stay.</param>
    public static void PruneOlderRecordings(string root, IReadOnlyCollection<string> keep)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentNullException.ThrowIfNull(keep);
        if (!Directory.Exists(root))
        {
            return;
        }

        var retained = new HashSet<string>(
            keep.Select(Path.GetFullPath),
            StringComparer.OrdinalIgnoreCase);
        foreach (var jsonPath in Directory.EnumerateFiles(root, "walkthrough.json", SearchOption.AllDirectories))
        {
            var directory = Path.GetDirectoryName(jsonPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            var full = Path.GetFullPath(directory);
            if (!retained.Contains(full))
            {
                TryDeleteDirectory(full);
            }
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    /// <summary>Replaces characters that cannot appear in a file name on Windows or Linux.</summary>
    /// <param name="value">Raw test or class name.</param>
    /// <returns>Safe folder segment.</returns>
    public static string Sanitize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var chars = value.Select(ch => IsUnsafeFileNameChar(ch) ? '_' : ch).ToArray();
        return new string(chars);
    }

    private static bool IsUnsafeFileNameChar(char ch) =>
        ch < 32 || ch is ' ' or '"' or '<' or '>' or '|' or ':' or '*' or '?' or '\\' or '/';
}
