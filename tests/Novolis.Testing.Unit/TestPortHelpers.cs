using System.Net;
using System.Net.Sockets;

namespace Novolis.Testing.Unit;

internal static class TestPortHelpers
{
    public static int FreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
