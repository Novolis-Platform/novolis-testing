using OpenQA.Selenium.Appium.Android;
using TUnit.Core;

namespace Novolis.Testing.Appium;

/// <summary>
/// Class-scoped Android Appium fixture. Inherit as <c>MyTests : AndroidAppiumTestBase&lt;MyTests&gt;</c>.
/// </summary>
/// <typeparam name="TSelf">The concrete test class.</typeparam>
public abstract class AndroidAppiumTestBase<TSelf>
    where TSelf : AndroidAppiumTestBase<TSelf>, new()
{
    private static AndroidAppiumSession? s_session;

    /// <summary>Session started by <see cref="StartSession"/>.</summary>
    protected static AndroidAppiumSession Session =>
        s_session ?? throw new InvalidOperationException("Android Appium session has not started.");

    /// <summary>Driver for the class-scoped session.</summary>
    protected static AndroidDriver Driver => Session.Driver;

    /// <summary>Builds session capabilities for this test class.</summary>
    /// <returns>Android session options.</returns>
    protected abstract AndroidAppiumSessionOptions CreateOptions();

    /// <summary>Connects once before tests in this class run.</summary>
    [Before(Class)]
    public static void StartSession()
    {
        s_session = AndroidAppiumSession.Connect(new TSelf().CreateOptions());
    }

    /// <summary>Disposes the class-scoped session.</summary>
    [After(Class)]
    public static void StopSession()
    {
        s_session?.Dispose();
        s_session = null;
    }
}
