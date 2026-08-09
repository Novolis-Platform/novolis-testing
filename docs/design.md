# Design

Logging test helpers, test bases, and Testcontainers integrations.

Published docs: [https://novolis-platform.github.io/.github/novolis-testing/](https://novolis-platform.github.io/.github/novolis-testing/)

## Layer placement

Follow [library-boundaries](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/library-boundaries.md) for layer placement.

## Goals

- Keep public APIs documented and packable as `Novolis.*` on GitHub Packages (when applicable).
- Prefer BCL types and existing Novolis packages over parallel abstractions.
- Document restore and ProjectReference-mode builds without local NuGet folder feeds.

## Non-goals

- Local NuGet folder feeds or committed cross-repo `ProjectReference` into sibling checkouts.
- Avalonia package references outside `Novolis.Avalonia.*`.
- Upward spine dependencies (e.g. Math → Simulation).

## Packages

- `Novolis.Testing.Coverage`
- `Novolis.Testing.Logging`
- `Novolis.Testing.ServiceBus`
- `Novolis.Testing.TestBases`
- `Novolis.Testing.Testcontainers`
- `Novolis.Testing.TestServer`
- `Novolis.Testing.TUnit`

## Topics

- `dotnet`
- `testing`
- `novolis`
