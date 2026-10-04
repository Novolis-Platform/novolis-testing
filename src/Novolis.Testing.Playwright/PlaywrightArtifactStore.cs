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
    /// Builds a unique folder for the current TUnit test, or <paramref name="testName"/> when supplied.
    /// </summary>
    /// <param name="testName">Optional test identity. When omitted, TUnit <see cref="TestContext"/> is used.</param>
    /// <param name="explicitRoot">Optional artifact root override.</param>
    /// <returns>Created directory path.</returns>
    public static string ForCurrentTest(string? testName = null, string? explicitRoot = null)
    {
        var metadata = TestContext.Current?.Metadata;
        var className = Sanitize(metadata?.TestDetails.ClassType.Name ?? "Tests");
        var name = Sanitize(testName ?? metadata?.TestName ?? "anonymous");
        var stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff");
        var directory = Path.Combine(ResolveRoot(explicitRoot), className, name, stamp);
        Directory.CreateDirectory(directory);
        Directory.CreateDirectory(Path.Combine(directory, "frames"));
        return directory;
    }

    /// <summary>Replaces characters that cannot appear in a Windows file name.</summary>
    /// <param name="value">Raw test or class name.</param>
    /// <returns>Safe folder segment.</returns>
    public static string Sanitize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(ch => invalid.Contains(ch) || ch is ' ' ? '_' : ch).ToArray();
        return new string(chars);
    }
}
