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

public sealed class WebApplicationTestBaseGuardTests
{
    [Test]
    public async Task GetServices_and_client_before_initialize_throw()
    {
        var subject = new ProbeWebApplicationTestBase();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
        {
            _ = subject.Services;
            return Task.CompletedTask;
        });
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
        {
            _ = subject.Client;
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task StartAsync_before_initialize_throws()
    {
        var subject = new ProbeWebApplicationTestBase();
        await Assert.ThrowsAsync<InvalidOperationException>(() => subject.StartAsync());
    }

    [Test]
    public async Task StopAsync_before_initialize_throws()
    {
        var subject = new ProbeWebApplicationTestBase();
        await Assert.ThrowsAsync<InvalidOperationException>(() => subject.StopAsync());
    }

    [Test]
    public async Task Constructor_with_logger_provider_registers_extra_provider()
    {
        var provider = new InMemoryLoggerProvider(
            Microsoft.Extensions.Options.Options.Create(new LoggerFilterOptions { MinLevel = LogLevel.Debug }));
        var subject = new ProbeWebApplicationTestBase(LogLevel.Debug, provider);
        await subject.InitializeAsync();
        try
        {
            await Assert.That(subject.Services).IsNotNull();
        }
        finally
        {
            await subject.DisposeAsync();
        }
    }

    private sealed class ProbeWebApplicationTestBase : WebApplicationTestBase
    {
        public ProbeWebApplicationTestBase(LogLevel logLevel = LogLevel.Error, ILoggerProvider? loggerProvider = null)
            : base(logLevel, loggerProvider)
        {
        }

        protected override int GetPort() => TestPortHelpers.FreeTcpPort();

        public IServiceProvider Services => GetServices;
        public HttpClient Client => GetTestClient;

        protected override Task SetupApplicationAsync(WebApplication application)
        {
            application.MapGet("/probe", () => "ok");
            return Task.CompletedTask;
        }
    }
}
