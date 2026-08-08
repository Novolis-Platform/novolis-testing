using Novolis.Testing.Coverage;

namespace Novolis.Testing.Unit;

public sealed class PublicApiSurfaceTests
{
    public sealed class SampleDto
    {
        public string? Name { get; set; }
    }

    public static class SampleFacade
    {
        public static int Answer() => 42;
    }

    [Test]
    public async Task PublicTypes_And_Methods_Are_Discoverable()
    {
        var asm = typeof(PublicApiSurfaceTests).Assembly;
        var types = PublicApiSurface.PublicTypes(asm);
        await Assert.That(types.Any(t => t == typeof(SampleDto))).IsTrue();

        var methods = PublicApiSurface.PublicMethods(typeof(SampleFacade));
        await Assert.That(methods.Any(m => m.Name == nameof(SampleFacade.Answer))).IsTrue();

        var flat = PublicApiSurface.PublicMethods(asm);
        await Assert.That(flat.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task SmokeInvoke_Runs_Parameterless_Static_And_Ctor()
    {
        var failures = PublicApiSurface.SmokeInvokeParameterless(typeof(PublicApiSurfaceTests).Assembly);
        // Other types in this assembly may fail smoke; SampleFacade.Answer and SampleDto ctor should not.
        await Assert.That(failures.Any(f => f.Contains(nameof(SampleFacade.Answer), StringComparison.Ordinal))).IsFalse();
        await Assert.That(failures.Any(f => f.Contains($"{nameof(SampleDto)}..ctor", StringComparison.Ordinal))).IsFalse();
    }
}
