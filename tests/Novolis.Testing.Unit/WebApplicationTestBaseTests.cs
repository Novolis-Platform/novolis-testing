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

public sealed class WebApplicationTestBaseTests : WebApplicationTestBase
{
    public WebApplicationTestBaseTests() : base(LogLevel.Warning)
    {
    }

    protected override int GetPort() => TestPortHelpers.FreeTcpPort();

    protected override Task SetupAsync(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(new Marker("web"));
        return Task.CompletedTask;
    }

    protected override Task SetupApplicationAsync(WebApplication application)
    {
        application.MapGet("/health", () => "ok");
        return Task.CompletedTask;
    }

    [Test]
    public async Task InitializeAsync_exposes_client_and_endpoints()
    {
        await InitializeAsync();
        try
        {
            var body = await GetTestClient.GetStringAsync("/health");
            await Assert.That(body).IsEqualTo("ok");

            var routes = GetEndpointRoutes.ToList();
            await Assert.That(routes).Contains("/health");

            var marker = GetServices.GetRequiredService<Marker>();
            await Assert.That(marker.Value).IsEqualTo("web");

            await StopAsync();
        }
        finally
        {
            await DisposeAsync();
        }
    }
}
