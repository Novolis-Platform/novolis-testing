<!-- novolis-pkg-brand:start -->
<p align="center">
  <a href="https://github.com/Novolis-Platform/novolis-testing">
    <img src="https://raw.githubusercontent.com/Novolis-Platform/.github/main/brand/logo-icon.svg" width="72" alt="Novolis"/>
  </a>
</p>
<!-- novolis-pkg-brand:end -->

# Novolis.Testing.ServiceBus

TUnit fixture for [AlmostServiceBus](https://github.com/gkinsman/AlmostServiceBus) via `Novolis.Messaging.ServiceBus.Broker.Almost` + `.Client`.

## Install

```bash
dotnet add package Novolis.Testing.ServiceBus
```

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0`). Local multi-repo builds: open `d:\novolis\Novolis.Platform.slnx` (ProjectReference mode) until Service Bus packages are on GitHub Packages.

## Quick start

```csharp
await using var bus = await ServiceBusTestHost.StartAsync();
await using var admin = bus.CreateAdministration();
await admin.EnsureQueueAsync("orders");

await using var client = bus.CreateClient();
await using var sender = client.CreateSender("orders");
await using var receiver = client.CreateReceiver("orders");

await sender.SendAsync(new Message<string>("hello"));
var received = await receiver.ReceiveAsync<string>(TimeSpan.FromSeconds(5));
await receiver.CompleteAsync(received!);
```

## Related

| Package | Role |
|---------|------|
| `Novolis.Messaging.ServiceBus.Client` | Azure SDK adapter |
| `Novolis.Messaging.ServiceBus.Broker.Almost` | Almost broker host |
| `Novolis.Testing.TestBases` | General TUnit host bases |
