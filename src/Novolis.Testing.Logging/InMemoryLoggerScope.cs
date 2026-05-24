namespace Novolis.Testing.Logging;

/// <summary>Logger scope state for <see cref="InMemoryLogger"/>.</summary>
/// <typeparam name="T">Scope state type.</typeparam>
public class InMemoryLoggerScope<T> : IDisposable
{
    /// <summary>Current scope state.</summary>
    public T? State { get; private set; }
    
    /// <summary>Creates a scope with the given state object.</summary>
    /// <param name="state">State value; must be assignable to <typeparamref name="T"/>.</param>
    public InMemoryLoggerScope(object state) => State = state is T t ? t : throw new ArgumentException($"The state must be of type {typeof(T).Name}");

    /// <inheritdoc />
    public void Dispose() => State = default;
}