using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Novolis.Testing.Logging;
using Novolis.Testing.TestBases;

namespace Novolis.Testing.Unit;

public sealed class SimpleTestLoggerProviderCtorTests
{
    [Test]
    public async Task Parameterless_options_ctor_uses_information_level()
    {
        var provider = new SimpleTestLoggerProvider(TestContext.Current!);
        var logger = provider.CreateLogger("cat");
        logger.LogInformation("visible");
        provider.Dispose();
        await Assert.That(logger.IsEnabled(LogLevel.Information)).IsTrue();
    }
}
