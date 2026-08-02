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

public sealed class HostApplicationTestBaseLifecycleTests
{
    [Test]
    public async Task GetServices_before_initialize_throws()
    {
        var subject = new ProbeHostApplicationTestBase();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
        {
            _ = subject.Services;
            return Task.CompletedTask;
        });
        await Assert.That(ex!.Message).Contains("not initialized");
    }

    [Test]
    public async Task DisposeHostAsync_without_initialize_is_noop()
    {
        var subject = new ProbeHostApplicationTestBase();
        await subject.PublicDisposeHostAsync();
    }

    [Test]
    public async Task Initialize_and_dispose_runs_full_lifecycle()
    {
        var subject = new ProbeHostApplicationTestBase();
        await subject.InitializeAsync();
        await Assert.That(subject.Services.GetService<string>()).IsEqualTo("configured");
        await subject.PublicDisposeHostAsync();
    }

    private sealed class ProbeHostApplicationTestBase : HostApplicationTestBase
    {
        public IServiceProvider Services => GetServices;

        protected override Task SetupAsync(HostApplicationBuilder builder)
        {
            builder.Services.AddSingleton("configured");
            return Task.CompletedTask;
        }

        public Task PublicDisposeHostAsync() => DisposeHostAsync();
    }
}

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

public sealed class TestOptionsTests
{
    [Test]
    public async Task Default_ctor_exposes_options()
    {
        var options = new TestOptions();
        await Assert.That(options.StartHost).IsFalse();
    }
}

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
