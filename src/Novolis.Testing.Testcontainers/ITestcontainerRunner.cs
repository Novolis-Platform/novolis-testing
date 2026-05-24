using DotNet.Testcontainers.Containers;

namespace Novolis.Testing.Testcontainers;

/// <summary>Abstraction for starting, stopping, and executing commands in a test container.</summary>
public interface ITestcontainerRunner : IAsyncDisposable
{
    /// <summary>Starts the container.</summary>
    Task StartAsync();
    
    /// <summary>Stops the container.</summary>
    Task StopAsync();
    
    /// <summary>Returns the current container state.</summary>
    TestcontainersStates GetState();
    
    /// <summary>Executes a shell command inside the container.</summary>
    /// <param name="command">Command line to run.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExecuteCommandAsync(string command, CancellationToken cancellationToken = default);
    
    /// <summary>Runs an arbitrary async action with container logging on failure.</summary>
    /// <param name="actionAsync">Action to execute.</param>
    Task ExecuteAsync(Func<Task> actionAsync);
}
