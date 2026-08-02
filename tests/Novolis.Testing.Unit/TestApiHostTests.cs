using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Novolis.Testing.TestServer;

namespace Novolis.Testing.Unit;

public sealed class TestApiHostTests
{
    [Test]
    public async Task ExecuteAsync_routes_stubbed_get_request()
    {
        await TestApiHost.Create()
            .With(HttpMethod.Get, "/ping", ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status200OK;
                return ctx.Response.WriteAsync("pong");
            })
            .Build(new Uri("http://127.0.0.1:0"))
            .ExecuteAsync(async client =>
            {
                var body = await client.GetStringAsync("/ping");
                await Assert.That(body).IsEqualTo("pong");
            });
    }

    [Test]
    public async Task WithMiddleware_adds_response_header()
    {
        await TestApiHost.Create()
            .With(HttpMethod.Get, "/ping", ctx => ctx.Response.WriteAsync("ok"))
            .WithMiddleware(next => async ctx =>
            {
                ctx.Response.Headers["x-test"] = "1";
                await next(ctx);
            })
            .Build(new Uri("http://127.0.0.1:0"))
            .ExecuteAsync(async client =>
            {
                using var response = await client.GetAsync("/ping");
                await Assert.That(response.Headers.TryGetValues("x-test", out var values)).IsTrue();
                await Assert.That(values!.Single()).IsEqualTo("1");
            });
    }

    [Test]
    public async Task Build_configure_registers_services_for_second_execute_overload()
    {
        await TestApiHost.Create()
            .With(HttpMethod.Get, "/svc", ctx => ctx.Response.WriteAsync("ok"))
            .Build(new Uri("http://127.0.0.1:0"), b => b.Services.AddSingleton(new MarkerService("wired")))
            .ExecuteAsync(async (_, services) =>
            {
                await Assert.That(services.GetService(typeof(MarkerService))).IsNotNull();
            });
    }

    private sealed record MarkerService(string Value);
}
