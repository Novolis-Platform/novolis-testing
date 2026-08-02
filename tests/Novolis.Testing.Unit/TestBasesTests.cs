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

internal sealed record Marker(string Value);

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

public sealed class StringExtensionsTests
{
    [Test]
    public async Task FirstToken_and_LastToken_split_on_char()
    {
        await Assert.That("a.b.c".FirstToken('.')).IsEqualTo("a");
        await Assert.That("a.b.c".LastToken('.')).IsEqualTo("c");
        await Assert.That("single".LastToken('.')).IsEqualTo("single");
    }
}

public sealed class TypeExtensionsPrimitiveTests
{
    [Test]
    public async Task GetFriendlyName_maps_primitives()
    {
        await Assert.That(typeof(short).GetFriendlyName()).IsEqualTo("Short");
        await Assert.That(typeof(int).GetFriendlyName()).IsEqualTo("Integer");
        await Assert.That(typeof(long).GetFriendlyName()).IsEqualTo("Long");
        await Assert.That(typeof(short?).GetDisplayName()).IsEqualTo("NullableNullableOfShort");
    }
}

public sealed class TestApiHostDisposeTests
{
    [Test]
    public async Task DisposeAsync_stops_host()
    {
        await using var host = TestApiHost.Create()
            .With(HttpMethod.Get, "/ping", ctx => ctx.Response.WriteAsync("ok"))
            .Build(new Uri("http://127.0.0.1:0"));

        await host.App.StartAsync();
        await host.DisposeAsync();
        await Assert.That(host.App).IsNotNull();
    }
}
