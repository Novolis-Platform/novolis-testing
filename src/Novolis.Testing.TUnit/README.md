# Novolis.Testing.TUnit

TUnit test output helpers: JSON dumps, tables, and C# literal formatting.

## Install

```bash
dotnet add package Novolis.Testing.TUnit
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), [TUnit](https://www.nuget.org/packages/TUnit).

## Quick start

```csharp
using Novolis.Testing.TUnit;
using TUnit.Core;

TestContext? output = TestContext.Current;
output.WriteLine("plain text");
output.WriteJson(new { Id = 1, Name = "alpha" });

var opts = output.GetDefaultJsonSerializerOptions();
```

Depends on `Novolis.CodeGen.Reflection.Dump` for rich object formatting.

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Testing.Logging` | Capture `ILogger` output in tests |
| `Novolis.Testing.TestBases` | ASP.NET Core / host bootstrapping |

## More documentation

- [Getting started](https://github.com/Novolis-Platform/novolis-testing/blob/main/docs/getting-started.md)
- [Design](https://github.com/Novolis-Platform/novolis-testing/blob/main/docs/design.md)

## Support

Pre-release (`2026.1.*` on GitHub Packages).
