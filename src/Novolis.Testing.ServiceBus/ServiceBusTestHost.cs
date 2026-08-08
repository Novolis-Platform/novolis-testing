using Novolis.Messaging.ServiceBus.Abstractions;
using Novolis.Messaging.ServiceBus.Broker.Almost;
using Novolis.Messaging.ServiceBus.Client;

namespace Novolis.Testing.ServiceBus;

/// <summary>
/// TUnit-friendly AlmostServiceBus host with per-instance namespace isolation.
/// Start, create Novolis clients, dispose.
/// </summary>
public sealed class ServiceBusTestHost : IAsyncDisposable
{
    private readonly AlmostServiceBusBroker _broker = new();

    /// <summary>Connection string for the isolated Almost namespace.</summary>
    public string ConnectionString => _broker.ConnectionString;

    /// <summary>Public AMQP/HTTP multiplex port.</summary>
    public int PublicPort => _broker.PublicPort;

    /// <summary>Isolated namespace name (SharedAccessKeyName).</summary>
    public string Namespace => _broker.Namespace;

    /// <summary>Client options pointed at this host.</summary>
    public ServiceBusClientOptions ClientOptions => _broker.CreateClientOptions();

    /// <summary>Starts a new in-process AlmostServiceBus fixture.</summary>
    public static async Task<ServiceBusTestHost> StartAsync(CancellationToken cancellationToken = default)
    {
        var host = new ServiceBusTestHost();
        await host._broker.StartAsync(cancellationToken).ConfigureAwait(false);
        return host;
    }

    /// <summary>Creates a Novolis <see cref="IServiceBusClient"/> against this host.</summary>
    public IServiceBusClient CreateClient() => new AzureServiceBusClient(ClientOptions);

    /// <summary>Creates administration client for queue ensure/create.</summary>
    public IServiceBusAdministration CreateAdministration() =>
        new AzureServiceBusAdministration(ClientOptions);

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _broker.DisposeAsync();
}
