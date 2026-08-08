using Novolis.Messaging.ServiceBus;
using Novolis.Testing.ServiceBus;

namespace Novolis.Testing.Unit;

public sealed class ServiceBusTestHostTests
{
    [Test]
    public async Task Start_create_client_and_admin_round_trip()
    {
        await using var host = await ServiceBusTestHost.StartAsync();
        await Assert.That(host.ConnectionString).IsNotNull().And.IsNotEmpty();
        await Assert.That(host.PublicPort).IsGreaterThan(0);
        await Assert.That(host.Namespace).IsNotNull().And.IsNotEmpty();
        await Assert.That(host.ClientOptions.ConnectionString).IsEqualTo(host.ConnectionString);

        await using var admin = host.CreateAdministration();
        var queue = $"q-{Guid.NewGuid():N}";
        await admin.EnsureQueueAsync(queue);

        await using var client = host.CreateClient();
        await using var sender = client.CreateSender(queue);
        await using var receiver = client.CreateReceiver(queue);

        await sender.SendAsync(new Message<string>("hello", subject: "t"));
        var received = await receiver.ReceiveAsync<string>(TimeSpan.FromSeconds(10));
        await Assert.That(received).IsNotNull();
        await Assert.That(received!.Payload).IsEqualTo("hello");
        await receiver.CompleteAsync(received);
    }
}
