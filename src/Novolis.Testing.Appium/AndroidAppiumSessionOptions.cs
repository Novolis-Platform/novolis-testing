using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Enums;

namespace Novolis.Testing.Appium;

/// <summary>
/// Capabilities for an Android UiAutomator2 session against an already running Appium server.
/// </summary>
public sealed class AndroidAppiumSessionOptions
{
    /// <summary>Appium server URI. When omitted, <see cref="AppiumServerAddress.Resolve"/> is used at connect time.</summary>
    public Uri? ServerUri { get; init; }

    /// <summary>Device name capability. Defaults to <c>Android Emulator</c>.</summary>
    public string DeviceName { get; init; } = "Android Emulator";

    /// <summary>Installed Android package name, for example <c>com.android.settings</c>.</summary>
    public string? AppPackage { get; init; }

    /// <summary>Activity to launch, for example <c>.Settings</c>.</summary>
    public string? AppActivity { get; init; }

    /// <summary>Path or URL of an APK when the app is not already installed.</summary>
    public string? AppPath { get; init; }

    /// <summary>When <see langword="true"/>, Appium skips reset of app state.</summary>
    public bool NoReset { get; init; }

    /// <summary>Maximum time to wait for each Appium command. Defaults to 180 seconds.</summary>
    public TimeSpan CommandTimeout { get; init; } = TimeSpan.FromSeconds(180);

    /// <summary>Implicit wait after the session starts. Defaults to 10 seconds.</summary>
    public TimeSpan ImplicitWait { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Builds Appium capabilities for UiAutomator2.
    /// </summary>
    /// <returns>Configured options.</returns>
    /// <exception cref="InvalidOperationException">Neither an APK path nor both package and activity were supplied.</exception>
    public AppiumOptions ToAppiumOptions()
    {
        var hasApp = !string.IsNullOrWhiteSpace(AppPath);
        var hasPackage = !string.IsNullOrWhiteSpace(AppPackage);
        var hasActivity = !string.IsNullOrWhiteSpace(AppActivity);
        if (!hasApp && !(hasPackage && hasActivity))
        {
            throw new InvalidOperationException(
                "Android Appium sessions require an APK path, or both appPackage and appActivity.");
        }

        var options = new AppiumOptions
        {
            AutomationName = AutomationName.AndroidUIAutomator2,
            PlatformName = MobilePlatform.Android,
            DeviceName = DeviceName,
        };

        if (hasApp)
            options.App = AppPath;

        if (hasPackage)
            options.AddAdditionalAppiumOption("appPackage", AppPackage!);

        if (hasActivity)
            options.AddAdditionalAppiumOption("appActivity", AppActivity!);

        if (NoReset)
            options.AddAdditionalAppiumOption("noReset", true);

        return options;
    }
}
