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
