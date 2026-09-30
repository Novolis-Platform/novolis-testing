using Novolis.Testing.Appium;
using TUnit.Core;

namespace Novolis.Testing.Unit;

public sealed class AppiumServerAddressTests
{
    [Test]
    public async Task Parse_accepts_absolute_uri()
    {
        var uri = AppiumServerAddress.Parse("http://127.0.0.1:4723/");
        await Assert.That(uri.ToString()).IsEqualTo("http://127.0.0.1:4723/");
    }

    [Test]
    public async Task Parse_rejects_relative_host()
    {
        await Assert.That(() => AppiumServerAddress.Parse("not-a-uri"))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Resolve_uses_explicit_host()
    {
        var uri = AppiumServerAddress.Resolve("http://10.0.0.8:4723/");
        await Assert.That(uri.ToString()).IsEqualTo("http://10.0.0.8:4723/");
    }

    [Test]
    [NotInParallel]
    public async Task Resolve_reads_environment_then_default()
    {
        var previous = Environment.GetEnvironmentVariable(AppiumServerAddress.HostEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(AppiumServerAddress.HostEnvironmentVariable, "http://192.0.2.10:4723/");
            var fromEnv = AppiumServerAddress.Resolve();
            await Assert.That(fromEnv.ToString()).IsEqualTo("http://192.0.2.10:4723/");

            Environment.SetEnvironmentVariable(AppiumServerAddress.HostEnvironmentVariable, null);
            var fromDefault = AppiumServerAddress.Resolve();
            await Assert.That(fromDefault.ToString()).IsEqualTo(AppiumServerAddress.DefaultHost);
        }
        finally
        {
            Environment.SetEnvironmentVariable(AppiumServerAddress.HostEnvironmentVariable, previous);
        }
    }
}
