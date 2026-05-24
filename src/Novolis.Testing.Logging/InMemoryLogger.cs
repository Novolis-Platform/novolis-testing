using Novolis.Testing.Internal;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Novolis.Testing.Logging;

/// <summary>In-memory <see cref="ILogger"/> that stores entries for assertions.</summary>
public class InMemoryLogger(IOptions<LoggerFilterOptions> options, string category) : ILogger
{
    private readonly List<InMemoryLogEntry> _logEntries = new();
    
    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) 
        => _logEntries.Add(new InMemoryLogEntry(logLevel,  eventId, exception, category, formatter(state, exception), state as IReadOnlyList<KeyValuePair<string, object?>>));

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => options.Value.Rules.Any(rule => rule.ProviderName == "InMemoryLogger" && rule.LogLevel <= logLevel);

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => new InMemoryLoggerScope<TState>(state);
    
    /// <summary>Returns all captured log entries.</summary>
    public IReadOnlyList<InMemoryLogEntry> GetLogEntries() => _logEntries;
}

/// <summary>Category-scoped in-memory logger for type <typeparamref name="T"/>.</summary>
/// <typeparam name="T">Logger category type.</typeparam>
public class InMemoryLogger<T> : InMemoryLogger, ILogger<T>
{
    /// <summary>Creates a logger for <typeparamref name="T"/>.</summary>
    /// <param name="options">Filter options.</param>
    public InMemoryLogger(IOptions<LoggerFilterOptions> options) : base(options, typeof(T).GetFullFriendlyName()) { }
}