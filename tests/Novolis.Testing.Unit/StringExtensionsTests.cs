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

public sealed class StringExtensionsTests
{
    [Test]
    public async Task FirstToken_and_LastToken_split_on_char()
    {
        await Assert.That("a.b.c".FirstToken('.')).IsEqualTo("a");
        await Assert.That("a.b.c".LastToken('.')).IsEqualTo("c");
        await Assert.That("single".LastToken('.')).IsEqualTo("single");
    }
}
