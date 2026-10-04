namespace Novolis.Testing.Playwright;

/// <summary>Stacked walkthrough scope. Dispose to leave the current flow.</summary>
public sealed class PlaywrightWalkthroughFlow : IDisposable
{
    private readonly Action onDispose;
    private bool disposed;

    internal PlaywrightWalkthroughFlow(Action onDispose)
    {
        this.onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        onDispose();
    }
}
