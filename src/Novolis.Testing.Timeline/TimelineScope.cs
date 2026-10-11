namespace Novolis.Testing.Timeline;

/// <summary>Disposable scope that leaves the current timeline section.</summary>
public sealed class TimelineScope : IDisposable
{
    private readonly Action onDispose;
    private bool disposed;

    internal TimelineScope(Action onDispose) =>
        this.onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));

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
