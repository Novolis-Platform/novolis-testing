using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Enums;

namespace Novolis.Testing.Appium;

/// <summary>
/// Capabilities for a Windows Appium session against an already running Appium server.
/// </summary>
public sealed class WindowsAppiumSessionOptions
{
    /// <summary>Appium server URI. When omitted, <see cref="AppiumServerAddress.Resolve"/> is used at connect time.</summary>
    public Uri? ServerUri { get; init; }

    /// <summary>Executable path or packaged application identifier.</summary>
    public required string App { get; init; }

    /// <summary>Device name capability. Defaults to <c>WindowsPC</c>.</summary>
    public string DeviceName { get; init; } = "WindowsPC";

    /// <summary>Automation name. Defaults to <c>Windows</c> for appium-windows-driver.</summary>
    public string AutomationName { get; init; } = "Windows";

    /// <summary>Maximum time to wait for each Appium command. Defaults to 180 seconds.</summary>
    public TimeSpan CommandTimeout { get; init; } = TimeSpan.FromSeconds(180);

    /// <summary>Implicit wait after the session starts. Defaults to 10 seconds.</summary>
    public TimeSpan ImplicitWait { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Builds Appium capabilities for Windows.
    /// </summary>
    /// <returns>Configured options.</returns>
    /// <exception cref="InvalidOperationException"><see cref="App"/> is missing.</exception>
    public AppiumOptions ToAppiumOptions()
    {
        if (string.IsNullOrWhiteSpace(App))
            throw new InvalidOperationException("Windows Appium sessions require an executable path or packaged app id.");

        return new AppiumOptions
        {
            AutomationName = AutomationName,
            PlatformName = MobilePlatform.Windows,
            DeviceName = DeviceName,
            App = App,
        };
    }
}
