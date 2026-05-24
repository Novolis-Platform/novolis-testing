using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TUnit.Core;

namespace Novolis.Testing.TestBases;

/// <summary>
/// Base for integration tests that start a generic host. TUnit runs <see cref="TUnitSetUp"/> / <see cref="TUnitTearDown"/> per test.
/// </summary>
public abstract class HostApplicationTestBase
{
    private readonly HostApplicationBuilder _hostApplicationBuilder;
    private IHost? _host;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _initialized;
    private IServiceScope? _scope;

    /// <summary>Creates the test host builder with the given logging defaults.</summary>
    /// <param name="logLevel">Minimum log level for the test host.</param>
    /// <param name="loggerProvider">Optional extra logger provider.</param>
    protected HostApplicationTestBase(LogLevel logLevel = LogLevel.Error, ILoggerProvider? loggerProvider = null)
    {
        _hostApplicationBuilder = Host.CreateApplicationBuilder();
        _hostApplicationBuilder.Logging.ClearProviders().AddDebug().SetMinimumLevel(logLevel);

        if (loggerProvider is not null)
            _hostApplicationBuilder.Logging.AddProvider(loggerProvider);
    }

    /// <summary>Scoped service provider after <see cref="InitializeAsync"/>.</summary>
    protected IServiceProvider GetServices =>
        _initialized
            ? _scope?.ServiceProvider ?? throw new InvalidOperationException("Host scope missing.")
            : throw new InvalidOperationException("Host not initialized. Ensure test inherits HostApplicationTestBase and TUnit hooks run.");

    /// <summary>Configures the host application builder before the host is built.</summary>
    /// <param name="builder">Application builder to configure.</param>
    /// <returns>A task that completes when setup finishes.</returns>
    protected virtual Task SetupAsync(HostApplicationBuilder builder) => Task.CompletedTask;

    /// <summary>Builds and starts the host.</summary>
    /// <returns>A task that completes when the host has started.</returns>
    public async Task InitializeAsync()
    {
        await SetupAsync(_hostApplicationBuilder);
        _host = _hostApplicationBuilder.Build();
        await _host.StartAsync(_cancellationTokenSource.Token);
        _scope = _host.Services.CreateScope();
        _initialized = true;
    }

    /// <summary>Stops and disposes the host.</summary>
    /// <returns>A task that completes when shutdown finishes.</returns>
    public async Task DisposeHostAsync()
    {
        if (_host is null)
            return;

        await _cancellationTokenSource.CancelAsync();
        await _host.StopAsync();
        await _host.WaitForShutdownAsync();
        _host.Dispose();
        _scope?.Dispose();
        _initialized = false;
        _host = null;
    }

    /// <summary>TUnit hook that starts the host before each test.</summary>
    [Before(Test)]
    public Task TUnitSetUp() => InitializeAsync();

    /// <summary>TUnit hook that disposes the host after each test.</summary>
    [After(Test)]
    public Task TUnitTearDown() => DisposeHostAsync();
}
