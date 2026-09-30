using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Novolis.Testing.Logging;
using Novolis.Testing.TestBases;

namespace Novolis.Testing.Unit;

public sealed class TestOptionsTests
{
    [Test]
    public async Task Default_ctor_exposes_options()
    {
        var options = new TestOptions();
        await Assert.That(options.StartHost).IsFalse();
    }
}
