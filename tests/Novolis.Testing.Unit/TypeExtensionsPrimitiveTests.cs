using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Novolis.Testing.Internal;
using Novolis.Testing.Logging;
using Novolis.Testing.TestBases;
using Novolis.Testing.TestServer;

namespace Novolis.Testing.Unit;

public sealed class TypeExtensionsPrimitiveTests
{
    [Test]
    public async Task GetFriendlyName_maps_primitives()
    {
        await Assert.That(typeof(short).GetFriendlyName()).IsEqualTo("Short");
        await Assert.That(typeof(int).GetFriendlyName()).IsEqualTo("Integer");
        await Assert.That(typeof(long).GetFriendlyName()).IsEqualTo("Long");
        await Assert.That(typeof(short?).GetDisplayName()).IsEqualTo("NullableNullableOfShort");
    }
}
