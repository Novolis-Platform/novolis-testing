using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Novolis.Testing.Internal;
using Novolis.Testing.Logging;
using Novolis.Testing.TestBases;
using Novolis.Testing.TestServer;

namespace Novolis.Testing.Unit;

public sealed class HostApplicationTestBaseTests : HostApplicationTestBase
{
    public HostApplicationTestBaseTests() : base(LogLevel.Warning, new InMemoryLoggerProvider(
        Options.Create(new LoggerFilterOptions { MinLevel = LogLevel.Debug })))
    {
    }

    protected override Task SetupAsync(HostApplicationBuilder builder)
    {
        builder.Services.AddSingleton(new Marker("host"));
        return Task.CompletedTask;
    }

    [Test]
    public async Task TUnit_hooks_expose_configured_services()
    {
        var marker = GetServices.GetRequiredService<Marker>();
        await Assert.That(marker.Value).IsEqualTo("host");
    }
}
