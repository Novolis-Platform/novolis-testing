namespace Novolis.Testing.Appium;

/// <summary>
/// Resolves the Appium server URI from an explicit host, <see cref="HostEnvironmentVariable"/>, or the local default.
/// </summary>
public static class AppiumServerAddress
{
    /// <summary>Environment variable that overrides the default Appium server URI.</summary>
    public const string HostEnvironmentVariable = "APPIUM_HOST";

    /// <summary>Default Appium server URI when no host is supplied.</summary>
    public const string DefaultHost = "http://127.0.0.1:4723/";

    /// <summary>
    /// Resolves an absolute Appium server URI.
    /// </summary>
    /// <param name="host">Explicit host. When omitted, <see cref="HostEnvironmentVariable"/> then <see cref="DefaultHost"/> are used.</param>
    /// <returns>Absolute server URI.</returns>
    /// <exception cref="InvalidOperationException">The resolved value is not an absolute URI.</exception>
    public static Uri Resolve(string? host = null)
    {
        var value = host
                    ?? Environment.GetEnvironmentVariable(HostEnvironmentVariable)
                    ?? DefaultHost;
        return Parse(value);
    }

    /// <summary>
    /// Parses a host string as an absolute Appium server URI.
    /// </summary>
    /// <param name="host">Host value, for example <c>http://127.0.0.1:4723/</c>.</param>
    /// <returns>Absolute server URI.</returns>
    /// <exception cref="ArgumentException"><paramref name="host"/> is missing.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="host"/> is not an absolute URI.</exception>
    public static Uri Parse(string host)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        if (!Uri.TryCreate(host, UriKind.Absolute, out var uri))
            throw new InvalidOperationException($"Appium host '{host}' is not an absolute URI.");

        return uri;
    }
}
