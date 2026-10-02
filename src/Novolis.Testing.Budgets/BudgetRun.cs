namespace Novolis.Testing.Budgets;

/// <summary>How many times a budget probe runs a caller-supplied body.</summary>
/// <param name="Probe">Highlight name for this workload.</param>
/// <param name="Iterations">Measured iterations. Warmup iterations are extra.</param>
/// <param name="Warmup">Unmeasured calls before the timed loop.</param>
/// <param name="OperationsPerIteration">Work units completed by one measured call. Throughput divides the total by elapsed seconds.</param>
/// <param name="Unit">Label for throughput, such as <c>slices/s</c> or <c>ops/s</c>.</param>
/// <param name="Parameters">Short description of the single case under test.</param>
/// <param name="Allocations">Allocation counter. Async probes must use <see cref="AllocationScope.Process"/>.</param>
public sealed record BudgetRun(
    string Probe,
    int Iterations,
    int Warmup = 1,
    long OperationsPerIteration = 1,
    string Unit = "ops/s",
    string Parameters = "",
    AllocationScope Allocations = AllocationScope.CurrentThread);
