using Novolis.Testing.Appium;

namespace Novolis.Testing.Unit;

public sealed class AndroidAppiumSessionOptionsTests
{
    [Test]
    public async Task ToAppiumOptions_sets_package_activity_and_defaults()
    {
        var options = new AndroidAppiumSessionOptions
        {
            AppPackage = "com.android.settings",
            AppActivity = ".Settings",
            NoReset = true,
        }.ToAppiumOptions();

        var capabilities = options.ToDictionary();
        await Assert.That(capabilities["platformName"]).IsEqualTo("Android");
        await Assert.That(capabilities["appium:automationName"]).IsEqualTo("UIAutomator2");
        await Assert.That(capabilities["appium:deviceName"]).IsEqualTo("Android Emulator");
        await Assert.That(capabilities["appium:appPackage"]).IsEqualTo("com.android.settings");
        await Assert.That(capabilities["appium:appActivity"]).IsEqualTo(".Settings");
        await Assert.That(Convert.ToBoolean(capabilities["appium:noReset"])).IsTrue();
    }

    [Test]
    public async Task ToAppiumOptions_sets_apk_path()
    {
        var options = new AndroidAppiumSessionOptions
        {
            AppPath = @"C:\apps\host.apk",
        }.ToAppiumOptions();

        var capabilities = options.ToDictionary();
        await Assert.That(capabilities["appium:app"]).IsEqualTo(@"C:\apps\host.apk");
    }

    [Test]
    public async Task ToAppiumOptions_rejects_missing_target()
    {
        await Assert.That(() => new AndroidAppiumSessionOptions().ToAppiumOptions())
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ToAppiumOptions_rejects_package_without_activity()
    {
        await Assert.That(() => new AndroidAppiumSessionOptions { AppPackage = "com.example" }.ToAppiumOptions())
            .Throws<InvalidOperationException>();
    }
}
