using OpenQA.Selenium.Appium.Windows;

namespace Novolis.Testing.Appium;

/// <summary>
/// Connected Windows Appium session. The Appium server must already be listening.
/// </summary>
public sealed class WindowsAppiumSession : IDisposable
{
    /// <summary>Creates a session around an existing driver.</summary>
    /// <param name="driver">Connected Windows driver.</param>
    public WindowsAppiumSession(WindowsDriver driver)
    {
        Driver = driver ?? throw new ArgumentNullException(nameof(driver));
    }

    /// <summary>Underlying Windows driver.</summary>
    public WindowsDriver Driver { get; }

    /// <summary>
    /// Connects to the Appium server and starts a Windows session.
    /// </summary>
    /// <param name="options">Session capabilities.</param>
    /// <returns>Connected session.</returns>
    public static WindowsAppiumSession Connect(WindowsAppiumSessionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var serverUri = options.ServerUri ?? AppiumServerAddress.Resolve();
        var driver = new WindowsDriver(serverUri, options.ToAppiumOptions(), options.CommandTimeout);
        try
        {
            driver.Manage().Timeouts().ImplicitWait = options.ImplicitWait;
            return new WindowsAppiumSession(driver);
        }
        catch
        {
            driver.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public void Dispose() => Driver.Dispose();
}
