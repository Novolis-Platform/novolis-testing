<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-testing">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Testing.Testcontainers

Opinionated Testcontainers runner with lifetime limits for integration tests.

## Install

```bash
dotnet add package Novolis.Testing.Testcontainers
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), Docker.

## Quick start

```csharp
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Builders;
using Novolis.Testing.Testcontainers;

ITestcontainerRunner runner = new TestContainerRunnerBuilder<PostgreSqlContainer>()
    .WithContainerFactory(() => new PostgreSqlBuilder().Build())
    .WithMaxLifetime(TimeSpan.FromMinutes(5))
    .Build();

await runner.StartAsync();
await runner.ExecuteCommandAsync("pg_isready");
await runner.StopAsync();
```

Pair with `Novolis.Testing.TestBases` for TUnit fixture wiring.

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Testing.TestBases` | Host + client setup |
| `Novolis.Testing.Logging` | Log container diagnostics |

## More documentation

- [Getting started](https://github.com/Novolis-Platform/novolis-testing/blob/main/docs/getting-started.md)
- [Design](https://github.com/Novolis-Platform/novolis-testing/blob/main/docs/design.md)

## Support

Pre-release (`2026.1.*` on GitHub Packages).

