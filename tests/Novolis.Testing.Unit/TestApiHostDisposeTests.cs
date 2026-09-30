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

public sealed class TestApiHostDisposeTests
{
    [Test]
    public async Task DisposeAsync_stops_host()
    {
        await using var host = TestApiHost.Create()
            .With(HttpMethod.Get, "/ping", ctx => ctx.Response.WriteAsync("ok"))
            .Build(new Uri("http://127.0.0.1:0"));

        await host.App.StartAsync();
        await host.DisposeAsync();
        await Assert.That(host.App).IsNotNull();
    }
}
