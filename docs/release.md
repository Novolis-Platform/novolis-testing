# Release

This repository publishes with the org CalVer scheme (`2026.1.*`) via `merge.yml` to GitHub Packages when packages are packable.

See [release-policy](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/release-policy.md).

Published docs: [https://novolis-platform.github.io/.github/novolis-testing/](https://novolis-platform.github.io/.github/novolis-testing/)

## Packages

- `Novolis.Testing.Coverage`
- `Novolis.Testing.Logging`
- `Novolis.Testing.ServiceBus`
- `Novolis.Testing.TestBases`
- `Novolis.Testing.Testcontainers`
- `Novolis.Testing.TestServer`
- `Novolis.Testing.TUnit`

## Consumers

Restore from nuget.org + `https://nuget.pkg.github.com/Novolis-Platform/index.json` only.

Local multi-repo iteration: open `d:\novolis\Novolis.Platform.slnx` (ProjectReference mode) — do not add a local feed.
