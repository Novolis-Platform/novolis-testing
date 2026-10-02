using System.Diagnostics;

namespace Novolis.Testing.Budgets;

/// <summary>Times a fixed iteration count and reads allocation counters.</summary>
public static class BudgetProbe
{
    /// <summary>Runs <paramref name="body"/> for the warmup, then measures the remaining iterations.</summary>
    /// <param name="run">Iteration count, operation count, and allocation counter.</param>
    /// <param name="body">Work to measure. Each call must start from the same inputs.</param>
    /// <returns>The measured sample.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="run"/> or <paramref name="body"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="run"/> has an empty probe name or unit.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Iterations or operations are not positive, or warmup is negative.</exception>
    /// <exception cref="InvalidOperationException">The allocation counter decreased.</exception>
    public static BudgetSample Measure(BudgetRun run, Action body)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(body);
        Validate(run);

        for (var index = 0; index < run.Warmup; index++)
            body();

        var gen0Before = GC.CollectionCount(0);
        var allocatedBefore = ReadAllocated(run.Allocations);
        var timestamp = Stopwatch.GetTimestamp();
        for (var index = 0; index < run.Iterations; index++)
            body();

        return Finish(run, timestamp, allocatedBefore, gen0Before);
    }

    /// <summary>Runs an async body for the warmup, then measures the remaining iterations.</summary>
    /// <param name="run">Iteration count, operation count, and allocation counter.</param>
    /// <param name="body">Work to measure. Each call must start from the same inputs.</param>
    /// <param name="cancellationToken">Cancels warmup and measured iterations.</param>
    /// <returns>The measured sample.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="run"/> or <paramref name="body"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="run"/> has an empty probe name or unit.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Iterations or operations are not positive, or warmup is negative.</exception>
    /// <exception cref="InvalidOperationException">
    /// The run asks for the current thread, or the allocation counter decreased.
    /// Async probes must use <see cref="AllocationScope.Process"/>.
    /// </exception>
    public static async Task<BudgetSample> MeasureAsync(
        BudgetRun run,
        Func<CancellationToken, Task> body,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(body);
        Validate(run);
        if (run.Allocations != AllocationScope.Process)
        {
            throw new InvalidOperationException(
                "Async budget probes must use AllocationScope.Process. Mark the test NotInParallel so other tests do not allocate in the same process.");
        }

        for (var index = 0; index < run.Warmup; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await body(cancellationToken).ConfigureAwait(false);
        }

        var gen0Before = GC.CollectionCount(0);
        var allocatedBefore = ReadAllocated(run.Allocations);
        var timestamp = Stopwatch.GetTimestamp();
        for (var index = 0; index < run.Iterations; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await body(cancellationToken).ConfigureAwait(false);
        }

        return Finish(run, timestamp, allocatedBefore, gen0Before);
    }

    private static void Validate(BudgetRun run)
    {
        if (string.IsNullOrWhiteSpace(run.Probe))
            throw new ArgumentException("The probe name is required.", nameof(run));

        if (string.IsNullOrWhiteSpace(run.Unit))
            throw new ArgumentException("The throughput unit is required.", nameof(run));

        if (run.Iterations <= 0)
            throw new ArgumentOutOfRangeException(nameof(run), "Measured iterations must be positive.");

        if (run.Warmup < 0)
            throw new ArgumentOutOfRangeException(nameof(run), "Warmup iterations cannot be negative.");

        if (run.OperationsPerIteration <= 0)
            throw new ArgumentOutOfRangeException(nameof(run), "Operations per iteration must be positive.");
    }

    private static BudgetSample Finish(BudgetRun run, long timestamp, long allocatedBefore, int gen0Before)
    {
        var elapsed = Stopwatch.GetElapsedTime(timestamp);
        var allocated = ReadAllocated(run.Allocations) - allocatedBefore;
        if (allocated < 0)
        {
            throw new InvalidOperationException(
                "Allocated bytes decreased. Keep a synchronous body on the calling thread, or use AllocationScope.Process on a test marked NotInParallel.");
        }

        var operations = run.Iterations * (double)run.OperationsPerIteration;
        var throughput = elapsed.TotalSeconds <= 0
            ? double.PositiveInfinity
            : operations / elapsed.TotalSeconds;

        return new BudgetSample(
            run.Probe,
            run.Parameters,
            elapsed,
            allocated,
            throughput,
            run.Unit,
            GC.CollectionCount(0) - gen0Before,
            Environment.WorkingSet);
    }

    private static long ReadAllocated(AllocationScope scope) =>
        scope == AllocationScope.Process
            ? GC.GetTotalAllocatedBytes(precise: true)
            : GC.GetAllocatedBytesForCurrentThread();
}
