using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Novolis.Testing.Logging;

/// <summary>Provides <see cref="SimpleTestLogger"/> instances writing to TUnit output.</summary>
public class SimpleTestLoggerProvider(TestContext outputHelper, IOptions<LoggerFilterOptions> options) : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, SimpleTestLogger> _loggers = new();
    
    /// <summary>Creates a provider with information-level default filtering.</summary>
    /// <param name="outputHelper">TUnit test context for output.</param>
    public SimpleTestLoggerProvider(TestContext outputHelper): this(outputHelper, Options.Create<LoggerFilterOptions>(new LoggerFilterOptions() { MinLevel = LogLevel.Information }))
    {
    }
    
    /// <inheritdoc />
    public void Dispose() => _loggers.Clear();

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => _loggers.GetOrAdd(categoryName, new SimpleTestLogger(outputHelper, options.Value.MinLevel, categoryName));
}