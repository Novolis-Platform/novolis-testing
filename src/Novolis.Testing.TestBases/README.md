# Novolis.Testing.TestBases

Reusable TUnit bases for `WebApplication` and generic `IHost` integration tests.

## Install

```bash
dotnet add package Novolis.Testing.TestBases
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## Quick start

```csharp
using Novolis.Testing.TestBases;

public sealed class HealthTests : WebApplicationTestBase
{
    protected override Task SetupAsync(WebApplicationBuilder builder)
    {
        builder.Services.AddControllers();
        return Task.CompletedTask;
    }

    [Test]
    public async Task Health_returns_ok()
    {
        await InitializeAsync();
        var response = await GetTestClient.GetAsync("/health");
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
    }
}
```

For non-HTTP hosts, inherit `HostApplicationTestBase` instead.

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Testing.TestServer` | Lightweight Kestrel stub routes |
| `Novolis.Testing.Logging` | In-memory log capture |

## More documentation

- [Getting started](https://github.com/Novolis-Platform/novolis-testing/blob/main/docs/getting-started.md)
- [Design](https://github.com/Novolis-Platform/novolis-testing/blob/main/docs/design.md)

## Support

Pre-release (`2026.1.*` on GitHub Packages).
