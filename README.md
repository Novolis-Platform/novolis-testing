<!-- novolis-marketing:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-brand-transparent.svg" width="360" alt="Novolis"/>
  </a>
</p>

<p align="center">
  <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/banners/novolis-testing.svg" width="100%" alt="novolis-testing"/>
</p>

<p align="center">
  <strong>Test bases and containers</strong><br/>
  Logging test helpers, test bases, and Testcontainers integrations.
</p>

<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-testing/actions"><img src="https://img.shields.io/github/actions/workflow/status/Novolis-Platform/novolis-testing/merge.yml?branch=main&label=merge&logo=github" alt="merge"/></a>
  <a href="https://github.com/orgs/Novolis-Platform/packages?repo_name=novolis-testing"><img src="https://img.shields.io/badge/packages-GitHub%20Packages-0a7ea3?logo=nuget" alt="packages"/></a>
  <a href="https://github.com/Novolis-Platform"><img src="https://img.shields.io/badge/org-Novolis--Platform-111827" alt="org"/></a>
</p>

<p align="center">
  <a href="https://nuget.pkg.github.com/Novolis-Platform/index.json"><code>https://nuget.pkg.github.com/Novolis-Platform/index.json</code></a>
  ·
  <a href="https://github.com/Novolis-Platform/.github/blob/main/profile/README.md">Org landing</a>
  ·
  <a href="https://github.com/Novolis-Platform/novolis-governance">Governance</a>
</p>

---
<!-- novolis-marketing:end -->
<!-- novolis-package-index:start -->
> **GitHub Packages shows this repository README on every package page** (upstream limitation).
> Open the **package README** for install and quick start — embedded in each .nupkg and linked below.

## Published packages

| Package | Install | Package README |
|---------|---------|----------------|
| `Novolis.Testing.Logging` | `dotnet add package Novolis.Testing.Logging` | [README](https://github.com/Novolis-Platform/novolis-testing/blob/main/src/Novolis.Testing.Logging/README.md) |
| `Novolis.Testing.TestBases` | `dotnet add package Novolis.Testing.TestBases` | [README](https://github.com/Novolis-Platform/novolis-testing/blob/main/src/Novolis.Testing.TestBases/README.md) |
| `Novolis.Testing.Testcontainers` | `dotnet add package Novolis.Testing.Testcontainers` | [README](https://github.com/Novolis-Platform/novolis-testing/blob/main/src/Novolis.Testing.Testcontainers/README.md) |
| `Novolis.Testing.TestServer` | `dotnet add package Novolis.Testing.TestServer` | [README](https://github.com/Novolis-Platform/novolis-testing/blob/main/src/Novolis.Testing.TestServer/README.md) |
| `Novolis.Testing.TUnit` | `dotnet add package Novolis.Testing.TUnit` | [README](https://github.com/Novolis-Platform/novolis-testing/blob/main/src/Novolis.Testing.TUnit/README.md) |

For NuGet.org and Visual Studio, the **embedded** README.md inside each package is authoritative.

<!-- novolis-package-index:end -->
# Testing

TUnit-first test helpers for Novolis library development.

## Packages

| Package | Purpose |
|---------|---------|
| `Novolis.Testing.TUnit` | Test output and assertion helpers |
| `Novolis.Testing.Logging` | Test logging utilities |
| `Novolis.Testing.TestBases` | Shared test base types |
| `Novolis.Testing.Testcontainers` | Testcontainers integration |
| `Novolis.Testing.TestServer` | In-process test server helpers |

## Install

```bash
dotnet add package Novolis.Testing.TUnit --version 0.1.0-preview.1
```

## Policy

TUnit only — no xUnit packages in this repo.

## Documentation

- [Getting started](docs/getting-started.md)
- [Design](docs/design.md)
- [Release](docs/release.md)

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

