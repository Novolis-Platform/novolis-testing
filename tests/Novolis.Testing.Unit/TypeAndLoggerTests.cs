using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Novolis.Testing.Internal;
using Novolis.Testing.Logging;

namespace Novolis.Testing.Unit;

public sealed class TypeExtensionsTests
{
    [Test]
    public async Task GetFriendlyName_OpenGeneric_UsesAngleBrackets()
    {
        await Assert.That(typeof(Dictionary<string, int>).GetFriendlyName())
            .IsEqualTo("Dictionary<String, Integer>");
        await Assert.That(typeof(Dictionary<string, int>).GetDisplayName())
            .IsEqualTo("DictionaryOfStringAndInteger");
    }

    [Test]
    public async Task GetFullFriendlyName_IncludesNamespace()
    {
        var name = typeof(List<string>).GetFullFriendlyName();
        await Assert.That(name).StartsWith("System.Collections.Generic.");
        await Assert.That(name).Contains("List<");
    }
}

public sealed class InMemoryLoggerTests
{
    [Test]
    public async Task Log_CapturesEntriesWhenEnabled()
    {
        var options = Options.Create(new LoggerFilterOptions
        {
            Rules =
            {
                new LoggerFilterRule("InMemoryLogger", null, LogLevel.Debug, null)
            }
        });
        var logger = new InMemoryLogger(options, "cat");
        await Assert.That(logger.IsEnabled(LogLevel.Information)).IsTrue();
        logger.LogInformation("hello {Name}", "world");
        var entries = logger.GetLogEntries();
        await Assert.That(entries).Count().IsEqualTo(1);
        await Assert.That(entries[0].Message).Contains("hello");
        await Assert.That(entries[0].CategoryName).IsEqualTo("cat");
        await Assert.That(entries[0].ToString()).Contains("hello");
    }

    [Test]
    public async Task AddInMemoryLoggingProvider_RegistersViaLoggingBuilder()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddLogging(b => b.AddInMemoryLoggingProvider());
        await using var sp = services.BuildServiceProvider();
        var factory = sp.GetRequiredService<ILoggerFactory>();
        var logger = factory.CreateLogger("unit");
        logger.LogWarning("captured");
        await Assert.That(logger).IsNotNull();
    }
}
