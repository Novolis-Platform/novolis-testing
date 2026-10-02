<!-- novolis-pkg-brand:start -->
[![Novolis](https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.png)](https://novolis-platform.github.io/.github/novolis-testing/)

[Novolis](https://github.com/Novolis-Platform) · [Docs](https://novolis-platform.github.io/.github/novolis-testing/) · [Source](https://github.com/Novolis-Platform/novolis-testing)
<!-- novolis-pkg-brand:end -->

# Novolis.Testing.Budgets

TUnit ceilings for elapsed time, allocated bytes, and throughput. A separate reader prints BenchmarkDotNet JSON in the same columns.

## Install

```bash
dotnet add package Novolis.Testing.Budgets
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), [TUnit](https://www.nuget.org/packages/TUnit).

## Quick start

```csharp
using Novolis.Testing.Budgets;
using TUnit.Core;

[NotInParallel("budgets")]
public sealed class SliceBudgetTests
{
    [Test]
    [Category("Budget")]
    public async Task FirstSlice_stays_within_ceiling()
    {
        var run = new BudgetRun(
            "TimeToFirstSlice",
            Iterations: 5,
            OperationsPerIteration: 1,
            Unit: "slices/s",
            Parameters: "4 MiB short/LF",
            Allocations: AllocationScope.Process);
        var sample = await BudgetProbe.MeasureAsync(run, _ => OpenAndReadAsync());
        await sample.AssertWithin(new Budget(MaxElapsed: TimeSpan.FromSeconds(2)));
    }

    private static Task OpenAndReadAsync() => Task.CompletedTask;
}
```

Synchronous probes may use `AllocationScope.CurrentThread`. Async probes use `AllocationScope.Process` and `[NotInParallel]`, because the process allocation counter includes every thread.

Profile executables stay in a repository `benchmarks/` folder and are not launched from merge CI. After a local BenchmarkDotNet run, `BenchmarkReport.TryRead` maps `*-report-full-compressed.json` onto the same highlight row. This package does not reference BenchmarkDotNet.

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Testing.TUnit` | JSON, XML, and table dumps inside a test |
| `Novolis.Testing.Logging` | Capture `ILogger` output in tests |

## More documentation

- [Getting started](https://github.com/Novolis-Platform/novolis-testing/blob/main/docs/getting-started.md)
- [Design](https://github.com/Novolis-Platform/novolis-testing/blob/main/docs/design.md)

## Support

Pre-release (`2026.1.*` on GitHub Packages).
