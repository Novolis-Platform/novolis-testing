using OpenQA.Selenium.Appium.Android;

namespace Novolis.Testing.Appium;

/// <summary>
/// Connected Android Appium session. The Appium server must already be listening.
/// </summary>
public sealed class AndroidAppiumSession : IDisposable
{
    /// <summary>Creates a session around an existing driver.</summary>
    /// <param name="driver">Connected Android driver.</param>
    public AndroidAppiumSession(AndroidDriver driver)
    {
        Driver = driver ?? throw new ArgumentNullException(nameof(driver));
    }

    /// <summary>Underlying UiAutomator2 driver.</summary>
    public AndroidDriver Driver { get; }

    /// <summary>
    /// Connects to the Appium server and starts an Android session.
    /// </summary>
    /// <param name="options">Session capabilities.</param>
    /// <returns>Connected session.</returns>
    public static AndroidAppiumSession Connect(AndroidAppiumSessionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var serverUri = options.ServerUri ?? AppiumServerAddress.Resolve();
        var driver = new AndroidDriver(serverUri, options.ToAppiumOptions(), options.CommandTimeout);
        try
        {
            driver.Manage().Timeouts().ImplicitWait = options.ImplicitWait;
            return new AndroidAppiumSession(driver);
        }
        catch
        {
            driver.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Starts an activity with the <c>mobile:startActivity</c> extension command.
    /// </summary>
    /// <param name="appPackage">Android package name.</param>
    /// <param name="appActivity">Activity name, for example <c>.Settings</c>.</param>
    public void StartActivity(string appPackage, string appActivity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appPackage);
        ArgumentException.ThrowIfNullOrWhiteSpace(appActivity);
        Driver.ExecuteScript("mobile:startActivity", AndroidStartActivityScript.Arguments(appPackage, appActivity));
    }

    /// <inheritdoc />
    public void Dispose() => Driver.Dispose();
}
