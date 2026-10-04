using Microsoft.Playwright;

namespace Novolis.Testing.Playwright;

/// <summary>
/// Installs Chromium through the Playwright .NET entry point. This is C# — it does not invoke Node.
/// </summary>
public static class PlaywrightBrowserInstaller
{
    private static readonly object Gate = new();
    private static bool s_attempted;
    private static bool s_available;

    /// <summary>
    /// Returns whether Chromium can be launched. Installs it once per process when missing.
    /// </summary>
    /// <returns><see langword="true"/> when Chromium is ready.</returns>
    public static bool EnsureChromium()
    {
        lock (Gate)
        {
            if (s_attempted)
            {
                return s_available;
            }

            s_attempted = true;
            try
            {
                var exit = Program.Main(["install", "chromium"]);
                s_available = exit == 0;
            }
            catch (Exception)
            {
                s_available = false;
            }

            return s_available;
        }
    }
}
