using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace Novolis.Testing.Logging;

/// <summary>Registers Novolis test logging providers.</summary>
public static class LoggingBuilderExtensions
{
    /// <summary>Adds <see cref="InMemoryLoggerProvider"/> with the given minimum level.</summary>
    public static ILoggingBuilder AddInMemoryLoggingProvider(this ILoggingBuilder builder, LogLevel logLevel = LogLevel.Debug)
    {
        builder.Services.Configure<LoggerFilterOptions>(options =>
        {
            options.MinLevel = logLevel;
        });
        builder.AddProvider<InMemoryLoggerProvider>();
        return builder;
    }
    
    /// <summary>Registers a custom <see cref="ILoggerProvider"/> type.</summary>
    /// <typeparam name="T">Provider implementation type.</typeparam>
    public static ILoggingBuilder AddProvider<T>(this ILoggingBuilder builder) where T : class, ILoggerProvider
    {
        builder.Services.AddSingleton<ILoggerProvider, T>();
        return builder;
    }
    
    /// <summary>Writes logs to TUnit <see cref="TestContext"/> output.</summary>
    public static ILoggingBuilder AddSimpleTestLogger(this ILoggingBuilder builder, LogLevel logLevel = LogLevel.Debug)
    {
        builder.Services.AddSingleton<ILoggerProvider, SimpleTestLoggerProvider>();
        builder.Services.Configure<LoggerFilterOptions>(options =>
        {
            options.MinLevel = logLevel;
        });
        return builder;
    }
}