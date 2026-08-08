# Novolis.Testing.Coverage

Helpers for **writing** coverage-closing tests (public API probes).

Cobertura **collection, gap analysis, and gates** live in
[`Novolis.Tools.Coverage`](https://github.com/Novolis-Platform/novolis-tools) —
CLIs (`novolis-coverage`) are thin orchestrators of that library.

## Install

```powershell
dotnet add package Novolis.Testing.Coverage --version 2026.1.*
```

## Public API smoke

```csharp
using Novolis.Testing.Coverage;

var asm = typeof(SomePublicType).Assembly;
foreach (var type in PublicApiSurface.PublicTypes(asm))
{
    _ = PublicApiSurface.PublicMethods(type);
}

var failures = PublicApiSurface.SmokeInvokeParameterless(asm);
// Inspect failures — many types need args; use for DTOs / parameterless facades.
```

## Policy

| Layer | Owns |
|-------|------|
| `Novolis.Tools.Coverage` | Collect, parse Cobertura, analyze gaps, fail-below gate |
| `Novolis.Tools.Coverage.Cli` | Thin CLI: `collect` / `list` / `gaps` |
| `Novolis.Testing.Coverage` | Public API enumeration / smoke helpers for unit tests |
| `novolis-governance` scripts | Policy + thin wrappers that call the tool |
