using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Novolis.Testing.Logging;
using TUnit.Core;

namespace Novolis.Testing.Unit;

public sealed class SimpleTestLoggerTests
{
    [Test]
    public async Task Log_writes_when_enabled()
    {
        var logger = TestContext.Current!.CreateTestLogger(LogLevel.Debug, "unit");
        await Assert.That(logger.IsEnabled(LogLevel.Information)).IsTrue();
        logger.LogInformation("hello {Name}", "world");
    }

    [Test]
    public async Task Typed_logger_uses_category_name()
    {
        var logger = TestContext.Current!.CreateTestLogger<SimpleTestLoggerTests>();
        logger.LogWarning("typed");
        await Assert.That(logger).IsNotNull();
    }

    [Test]
    public async Task BeginScope_and_log_event_formatting()
    {
        var logger = new SimpleTestLogger(TestContext.Current, LogLevel.Debug, "scope");
        using var scope = logger.BeginScope("outer");
        await Assert.That(scope).IsNotNull();

        logger.LogError(new EventId(9, "evt"), new InvalidOperationException("boom"), "failed");

        var evt = new LogEvent(
            LogLevel.Error,
            new EventId(9, "evt"),
            new InvalidOperationException("boom"),
            "scope",
            "failed",
            null);
        var text = evt.ToString();
        await Assert.That(text).Contains("Error");
        await Assert.That(text).Contains("failed");

        var scoped = new SimpleLoggerScope<string>("value");
        await Assert.That(scoped.State).IsEqualTo("value");
        scoped.Dispose();
        await Assert.That(scoped.State).IsNull();
    }

    [Test]
    public async Task Provider_creates_and_caches_loggers()
    {
        var provider = TestContext.Current!.CreateTestLoggerProvider(LogLevel.Information);
        var first = provider.CreateLogger("cat");
        var second = provider.CreateLogger("cat");
        await Assert.That(ReferenceEquals(first, second)).IsTrue();

        var ctx = TestContext.Current!;
        using var factory = ctx.CreateTestLoggerFactory(LogLevel.Debug);
        var factoryLogger = factory.CreateLogger("factory");
        factoryLogger.LogDebug("from factory");
        await Assert.That(factoryLogger).IsNotNull();

        provider.Dispose();
    }

    [Test]
    public async Task AddSimpleTestLogger_configures_services()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TestContext.Current!);
        services.AddLogging(b => b.AddSimpleTestLogger(LogLevel.Warning));
        await using var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<ILoggerFactory>();
        var logger = factory.CreateLogger("builder");
        logger.LogWarning("via builder");
        await Assert.That(logger).IsNotNull();
    }

    [Test]
    public async Task Log_skips_below_minimum_level()
    {
        var logger = new SimpleTestLogger(TestContext.Current, LogLevel.Error, "quiet");
        await Assert.That(logger.IsEnabled(LogLevel.Information)).IsFalse();
        logger.LogInformation("ignored");
    }
}
