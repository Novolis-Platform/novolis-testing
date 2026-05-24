using DotNet.Testcontainers.Containers;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Novolis.Testing.Testcontainers;

/// <summary>Fluent builder for <see cref="ContainerRunner{T}"/> instances.</summary>
/// <typeparam name="T">Container type implementing <see cref="IContainer"/>.</typeparam>
public class TestContainerRunnerBuilder<T> where T : class, IContainer
{
    private ILogger<T>? _logger;
    private TimeSpan _maxLifetime;
    private CancellationToken _cancellationToken;
    private Func<IContainer>? _containerFactory;

    /// <summary>Sets the logger used by the runner.</summary>
    /// <param name="logger">Logger instance, or <see langword="null"/> for a null logger.</param>
    /// <returns>This builder.</returns>
    public TestContainerRunnerBuilder<T> WithLogger(ILogger<T>? logger)
    {
        _logger = logger;
        return this;
    }

    /// <summary>Sets the maximum container lifetime.</summary>
    /// <param name="maxLifetime">Lifetime before automatic cancellation.</param>
    /// <returns>This builder.</returns>
    public TestContainerRunnerBuilder<T> WithMaxLifetime(TimeSpan maxLifetime)
    {
        _maxLifetime = maxLifetime;
        return this;
    }

    /// <summary>Sets the cancellation token linked to container lifetime.</summary>
    /// <param name="cancellationToken">External cancellation token.</param>
    /// <returns>This builder.</returns>
    public TestContainerRunnerBuilder<T> WithCancellationToken(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        return this;
    }

    /// <summary>Sets the factory that creates the underlying container.</summary>
    /// <param name="containerFactory">Factory delegate.</param>
    /// <returns>This builder.</returns>
    public TestContainerRunnerBuilder<T> WithContainerFactory(Func<IContainer>? containerFactory)
    {
        _containerFactory = containerFactory;
        return this;
    }

    /// <summary>Builds an <see cref="ITestcontainerRunner"/> with configured options.</summary>
    /// <returns>Configured container runner.</returns>
    public ITestcontainerRunner Build()
    {
        _logger ??= new NullLogger<T>();
        if (_containerFactory == null)
            throw new ArgumentNullException(nameof(_containerFactory));
        if (_maxLifetime == default)
            _maxLifetime = TimeSpan.FromMinutes(1);
        if (_cancellationToken == default)
            _cancellationToken = CancellationToken.None;
        
        return new ContainerRunner<IContainer>(_logger, _containerFactory(), _maxLifetime, _cancellationToken);
    }
}
