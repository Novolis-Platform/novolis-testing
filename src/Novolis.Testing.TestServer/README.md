# Novolis.Testing.TestServer

Fluent Kestrel test host builder for in-process HTTP integration tests.

## Install

```bash
dotnet add package Novolis.Testing.TestServer
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## Quick start

```csharp
using Novolis.Testing.TestServer;

await TestApiHost.Create()
    .With(HttpMethod.Get, "/ping", ctx =>
    {
        ctx.Response.StatusCode = 200;
        return Task.CompletedTask;
    })
    .Build(new Uri("http://127.0.0.1:0"))
    .ExecuteAsync(async client =>
    {
        var body = await client.GetStringAsync("/ping");
        await Assert.That(body).IsEqualTo("ok");
    });
```

Use when you need a minimal API surface without a full `WebApplication` project.

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Testing.TestBases` | TUnit lifecycle around `WebApplicationFactory`-style setup |
| `Novolis.Testing.Testcontainers` | Real databases and brokers in Docker |

## More documentation

- [Getting started](https://github.com/Novolis-Platform/novolis-testing/blob/main/docs/getting-started.md)
- [Design](https://github.com/Novolis-Platform/novolis-testing/blob/main/docs/design.md)

## Support

Pre-release (`2026.1.*` on GitHub Packages).
