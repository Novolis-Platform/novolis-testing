using Novolis.Testing.Appium;

namespace Novolis.Testing.Unit;

public sealed class WindowsAppiumSessionOptionsTests
{
    [Test]
    public async Task ToAppiumOptions_sets_windows_defaults()
    {
        var options = new WindowsAppiumSessionOptions
        {
            App = @"C:\apps\Merglyph.exe",
        }.ToAppiumOptions();

        var capabilities = options.ToDictionary();
        await Assert.That(capabilities["platformName"]).IsEqualTo("Windows");
        await Assert.That(capabilities["appium:automationName"]).IsEqualTo("Windows");
        await Assert.That(capabilities["appium:deviceName"]).IsEqualTo("WindowsPC");
        await Assert.That(capabilities["appium:app"]).IsEqualTo(@"C:\apps\Merglyph.exe");
    }

    [Test]
    public async Task ToAppiumOptions_rejects_missing_app()
    {
        await Assert.That(() => new WindowsAppiumSessionOptions { App = " " }.ToAppiumOptions())
            .Throws<InvalidOperationException>();
    }
}
