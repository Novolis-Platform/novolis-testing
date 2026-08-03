<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-testing">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Testing.Logging

In-memory and TUnit-friendly logging providers for tests.

## Install

```bash
dotnet add package Novolis.Testing.Logging
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`).

## Quick start

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Novolis.Testing.Logging;

builder.Logging.AddInMemoryLoggingProvider(LogLevel.Debug);

var logger = services.GetRequiredService<ILogger<MyService>>();
logger.LogInformation("captured");

var mem = (InMemoryLogger)services.GetRequiredService<ILogger<MyService>>();
IReadOnlyList<InMemoryLogEntry> entries = mem.GetLogEntries();
```

Use `AddSimpleTestLogger` to mirror logs into TUnit `TestContext` output.

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Testing.TUnit` | JSON/table dump helpers for assertions |
| `Novolis.Testing.TestBases` | Web and generic host test bases |

## More documentation

- [Getting started](https://github.com/Novolis-Platform/novolis-testing/blob/main/docs/getting-started.md)
- [Design](https://github.com/Novolis-Platform/novolis-testing/blob/main/docs/design.md)

## Support

Pre-release (`2026.1.*` on GitHub Packages).

