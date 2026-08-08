using System.Reflection;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Novolis.Testing.Internal;
using Novolis.Testing.Logging;
using Novolis.Testing.TestBases;

namespace Novolis.Testing.Unit;

public sealed class CoverageBranchGapTests
{
    [Test]
    public async Task FirstToken_without_split_returns_whole_string()
    {
        await Assert.That("nosplit".FirstToken('.')).IsEqualTo("nosplit");
        await Assert.That("a.b".LastToken('/')).IsEqualTo("a.b");
    }

    [Test]
    public async Task GetFullDisplayName_without_namespace_omits_prefix()
    {
        var type = EmitTypeWithoutNamespace("GapType");
        await Assert.That(type.GetFullDisplayName()).IsEqualTo("GapType");
        await Assert.That(type.GetFullFriendlyName()).IsEqualTo("GapType");
    }

    [Test]
    public async Task SimpleTestLogger_null_output_helper_still_logs()
    {
        var logger = new SimpleTestLogger(null, LogLevel.Debug, "cat");
        logger.LogInformation("silent");
        await Assert.That(logger.IsEnabled(LogLevel.Debug)).IsTrue();
        await Assert.That(logger.IsEnabled(LogLevel.Trace)).IsFalse();
    }

    [Test]
    public async Task InMemoryLoggerScope_wrong_state_type_throws()
    {
        await Assert.That(() => new InMemoryLoggerScope<int>("not-an-int"))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task WebApplication_StopAsync_after_initialize()
    {
        var subject = new ProbeWeb();
        await subject.InitializeAsync();
        try
        {
            var response = await subject.Client.GetStringAsync("/probe");
            await Assert.That(response).IsEqualTo("ok");
            await subject.StopAsync();
        }
        finally
        {
            await subject.DisposeAsync();
        }
    }

    [Test]
    public async Task WebApplication_default_GetPort_is_in_ephemeral_range()
    {
        var subject = new DefaultPortProbe();
        await Assert.That(subject.PublicGetPort()).IsGreaterThanOrEqualTo(5000);
        await Assert.That(subject.PublicGetPort()).IsLessThanOrEqualTo(6000);
    }

    [Test]
    public async Task HostApplication_logger_provider_ctor_and_default_setup()
    {
        var provider = new InMemoryLoggerProvider(
            Microsoft.Extensions.Options.Options.Create(new LoggerFilterOptions { MinLevel = LogLevel.Warning }));
        var subject = new ProbeHost(LogLevel.Warning, provider);
        await subject.InitializeAsync();
        try
        {
            await Assert.That(subject.Services).IsNotNull();
        }
        finally
        {
            await subject.PublicDisposeHostAsync();
        }
    }

    private static Type EmitTypeWithoutNamespace(string name)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("Novolis.Testing.GapEmit"),
            AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("m");
        var typeBuilder = module.DefineType(name, TypeAttributes.Public | TypeAttributes.Class);
        return typeBuilder.CreateType()!;
    }

    private sealed class ProbeWeb : WebApplicationTestBase
    {
        protected override int GetPort() => TestPortHelpers.FreeTcpPort();

        public HttpClient Client => GetTestClient;

        protected override Task SetupApplicationAsync(WebApplication application)
        {
            application.MapGet("/probe", () => "ok");
            return Task.CompletedTask;
        }
    }

    private sealed class DefaultPortProbe : WebApplicationTestBase
    {
        public int PublicGetPort() => GetPort();
    }

    private sealed class ProbeHost : HostApplicationTestBase
    {
        public ProbeHost(LogLevel logLevel, ILoggerProvider loggerProvider)
            : base(logLevel, loggerProvider)
        {
        }

        public IServiceProvider Services => GetServices;

        public Task PublicDisposeHostAsync() => DisposeHostAsync();
    }
}
