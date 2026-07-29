using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Novolis.Testing.Internal;
using Novolis.Testing.Logging;
using Novolis.Testing.TUnit;

namespace Novolis.Testing.Unit;

public sealed class TypeExtensionsAdditionalTests
{
    [Test]
    public async Task GetFriendlyName_NullableInteger()
    {
        await Assert.That(typeof(int?).GetFriendlyName()).IsEqualTo("NullableNullable<Integer>");
        await Assert.That(typeof(int?).GetDisplayName()).IsEqualTo("NullableNullableOfInteger");
    }

    [Test]
    public async Task GetFriendlyName_NestedGeneric()
    {
        var name = typeof(Dictionary<string, List<int>>).GetFriendlyName();
        await Assert.That(name).IsEqualTo("Dictionary<String, List<Integer>>");
    }

    [Test]
    public async Task GetFullDisplayName_IncludesNamespace()
    {
        var name = typeof(List<string>).GetFullDisplayName();
        await Assert.That(name).StartsWith("System.Collections.Generic.");
        await Assert.That(name).Contains("ListOfString");
    }
}

public sealed class InMemoryLoggingAdditionalTests
{
    [Test]
    public async Task InMemoryLogger_TypedCategory_UsesFullFriendlyName()
    {
        var options = Options.Create(new LoggerFilterOptions
        {
            Rules = { new LoggerFilterRule("InMemoryLogger", null, LogLevel.Debug, null) }
        });
        var logger = new InMemoryLogger<InMemoryLoggingAdditionalTests>(options);
        logger.LogError("typed category");

        var entry = logger.GetLogEntries().Single();
        await Assert.That(entry.CategoryName).Contains(nameof(InMemoryLoggingAdditionalTests));
    }

    [Test]
    public async Task InMemoryLogEntry_ToString_IncludesException()
    {
        var entry = new InMemoryLogEntry(
            LogLevel.Error,
            new EventId(7, "evt"),
            new InvalidOperationException("boom"),
            "tests",
            "failed",
            null);

        var text = entry.ToString();
        await Assert.That(text).Contains("Error");
        await Assert.That(text).Contains("failed");
        await Assert.That(text).Contains("InvalidOperationException");
    }

    [Test]
    public async Task BeginScope_CreatesTypedScope()
    {
        var options = Options.Create(new LoggerFilterOptions
        {
            Rules = { new LoggerFilterRule("InMemoryLogger", null, LogLevel.Debug, null) }
        });
        var logger = new InMemoryLogger(options, "scope");
        using var scope = logger.BeginScope("outer");
        await Assert.That(scope).IsNotNull();

        var typedScope = new InMemoryLoggerScope<string>("value");
        await Assert.That(typedScope.State).IsEqualTo("value");
        typedScope.Dispose();
        await Assert.That(typedScope.State).IsNull();
    }

    [Test]
    public async Task GetDefaultJsonSerializerOptions_UsesCamelCase()
    {
        var options = ((TestContext?)null).GetDefaultJsonSerializerOptions();
        await Assert.That(options.PropertyNamingPolicy).IsEqualTo(JsonNamingPolicy.CamelCase);
        await Assert.That(options.WriteIndented).IsTrue();
    }
}
