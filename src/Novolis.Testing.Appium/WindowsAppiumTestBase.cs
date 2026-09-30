using OpenQA.Selenium.Appium.Windows;
using TUnit.Core;

namespace Novolis.Testing.Appium;

/// <summary>
/// Class-scoped Windows Appium fixture. Inherit as <c>MyTests : WindowsAppiumTestBase&lt;MyTests&gt;</c>.
/// </summary>
/// <typeparam name="TSelf">The concrete test class.</typeparam>
public abstract class WindowsAppiumTestBase<TSelf>
    where TSelf : WindowsAppiumTestBase<TSelf>, new()
{
    private static WindowsAppiumSession? s_session;

    /// <summary>Session started by <see cref="StartSession"/>.</summary>
    protected static WindowsAppiumSession Session =>
        s_session ?? throw new InvalidOperationException("Windows Appium session has not started.");

    /// <summary>Driver for the class-scoped session.</summary>
    protected static WindowsDriver Driver => Session.Driver;

    /// <summary>Builds session capabilities for this test class.</summary>
    /// <returns>Windows session options.</returns>
    protected abstract WindowsAppiumSessionOptions CreateOptions();

    /// <summary>Connects once before tests in this class run.</summary>
    [Before(Class)]
    public static void StartSession()
    {
        s_session = WindowsAppiumSession.Connect(new TSelf().CreateOptions());
    }

    /// <summary>Disposes the class-scoped session.</summary>
    [After(Class)]
    public static void StopSession()
    {
        s_session?.Dispose();
        s_session = null;
    }
}
