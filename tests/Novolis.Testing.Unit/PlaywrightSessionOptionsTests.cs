using Novolis.Testing.Playwright;

namespace Novolis.Testing.Unit;

public sealed class PlaywrightSessionOptionsTests
{
    [Test]
    public async Task Defaults_run_headless_and_hold_recording_frames()
    {
        var options = new PlaywrightSessionOptions();
        await Assert.That(options.Headless).IsTrue();
        await Assert.That(options.RecordVideo).IsFalse();
        await Assert.That(options.RecordTrace).IsTrue();
        await Assert.That(options.FrameHoldMilliseconds).IsEqualTo(4000);
        await Assert.That(options.ViewportWidth).IsEqualTo(1440);
        await Assert.That(options.ViewportHeight).IsEqualTo(900);
    }

    [Test]
    [NotInParallel]
    public async Task ResolveHeadless_honours_headed_environment()
    {
        var previous = Environment.GetEnvironmentVariable("NOVOLIS_PLAYWRIGHT_HEADED");
        try
        {
            Environment.SetEnvironmentVariable("NOVOLIS_PLAYWRIGHT_HEADED", "1");
            await Assert.That(new PlaywrightSessionOptions().ResolveHeadless()).IsFalse();
            Environment.SetEnvironmentVariable("NOVOLIS_PLAYWRIGHT_HEADED", null);
            await Assert.That(new PlaywrightSessionOptions().ResolveHeadless()).IsTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("NOVOLIS_PLAYWRIGHT_HEADED", previous);
        }
    }
}
