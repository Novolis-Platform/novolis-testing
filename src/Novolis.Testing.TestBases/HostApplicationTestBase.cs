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

    protected HostApplicationTestBase(LogLevel logLevel = LogLevel.Error, ILoggerProvider? loggerProvider = null)
    {
        _hostApplicationBuilder = Host.CreateApplicationBuilder();
        _hostApplicationBuilder.Logging.ClearProviders().AddDebug().SetMinimumLevel(logLevel);

        if (loggerProvider is not null)
            _hostApplicationBuilder.Logging.AddProvider(loggerProvider);
    }

    protected IServiceProvider GetServices =>
        _initialized
            ? _scope?.ServiceProvider ?? throw new InvalidOperationException("Host scope missing.")
            : throw new InvalidOperationException("Host not initialized. Ensure test inherits HostApplicationTestBase and TUnit hooks run.");

    protected virtual Task SetupAsync(HostApplicationBuilder builder) => Task.CompletedTask;

    public async Task InitializeAsync()
    {
        await SetupAsync(_hostApplicationBuilder);
        _host = _hostApplicationBuilder.Build();
        await _host.StartAsync(_cancellationTokenSource.Token);
        _scope = _host.Services.CreateScope();
        _initialized = true;
    }

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

    [Before(Test)]
    public Task TUnitSetUp() => InitializeAsync();

    [After(Test)]
    public Task TUnitTearDown() => DisposeHostAsync();
}
