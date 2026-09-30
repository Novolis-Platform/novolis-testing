<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-testing">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Testing.Appium

TUnit helpers that open Android and Windows Appium sessions for MAUI hosts. The C# client connects to an already running Appium server. This package does not reference `Microsoft.Maui.*` and does not start Appium, install drivers, or launch emulators.

## Install

```bash
dotnet add package Novolis.Testing.Appium
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`), [TUnit](https://www.nuget.org/packages/TUnit), [Appium server](https://appium.io/docs/en/latest/quickstart/test-dotnet/) already listening (default `http://127.0.0.1:4723/`). Install UiAutomator2 and/or the Windows driver on that server outside this repo. Override the URI with `APPIUM_HOST`.

## Quick start (Android)

```csharp
using Novolis.Testing.Appium;
using TUnit.Core;

public sealed class SettingsTests : AndroidAppiumTestBase<SettingsTests>
{
    protected override AndroidAppiumSessionOptions CreateOptions() => new()
    {
        AppPackage = "com.android.settings",
        AppActivity = ".Settings",
        NoReset = true,
    };

    [Test]
    public async Task Apps_row_opens()
    {
        Session.StartActivity("com.android.settings", ".Settings");
        await Assert.That(Driver).IsNotNull();
    }
}
```

Point `AppPath` at a built APK when the package is not already on the device. `StartActivity` sends `mobile:startActivity` (the 5.x `AndroidDriver.StartActivity(package, activity)` overload is gone).

## Quick start (Windows)

```csharp
using Novolis.Testing.Appium;

public sealed class HostTests : WindowsAppiumTestBase<HostTests>
{
    protected override WindowsAppiumSessionOptions CreateOptions() => new()
    {
        App = @"C:\apps\Merglyph.exe",
    };
}
```

`App` may be an exe path or a packaged application id. The server must run `appium-windows-driver`; this library only sets capabilities.

## Related packages

| Package | When to use |
|---------|-------------|
| `Novolis.Testing.TestBases` | In-process host fixtures (not device UI) |
| `Novolis.Testing.TUnit` | JSON/table dump helpers for assertions |

## More documentation

- [Getting started](https://github.com/Novolis-Platform/novolis-testing/blob/main/docs/getting-started.md)
- [Design](https://github.com/Novolis-Platform/novolis-testing/blob/main/docs/design.md)

## Support

Pre-release (`2026.1.*` on GitHub Packages).
