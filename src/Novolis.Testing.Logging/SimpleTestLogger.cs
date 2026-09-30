using Novolis.Testing.Internal;

using Microsoft.Extensions.Logging;


namespace Novolis.Testing.Logging;

/// <summary>Writes logs to TUnit output for category <typeparamref name="T"/>.</summary>
/// <typeparam name="T">Logger category type.</typeparam>
public class SimpleTestLogger<T>(TestContext? outputHelper, LogLevel logLevel) : SimpleTestLogger(outputHelper, logLevel, typeof(T).GetDisplayName()), ILogger<T>;

/// <summary>Writes formatted log events to a TUnit <see cref="TestContext"/>.</summary>
public class SimpleTestLogger(TestContext? outputHelper, LogLevel level, string categoryName) : ILogger
{
    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull 
        => new SimpleLoggerScope<TState>(state);

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => logLevel >= level;

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (logLevel < level)
            return;

        outputHelper?.OutputWriter.WriteLine(new LogEvent(logLevel, eventId, exception, categoryName, formatter.Invoke(state, exception), state as IReadOnlyList<KeyValuePair<string, object?>>).ToString());
    }
}
