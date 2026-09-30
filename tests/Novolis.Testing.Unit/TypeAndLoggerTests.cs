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
