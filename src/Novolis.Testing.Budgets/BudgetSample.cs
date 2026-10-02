namespace Novolis.Testing.Budgets;

/// <summary>One measured run of a <see cref="BudgetRun"/>.</summary>
/// <param name="Probe">Highlight name.</param>
/// <param name="Parameters">Case description supplied by the caller.</param>
/// <param name="Elapsed">Time spent in the measured iterations.</param>
/// <param name="AllocatedBytes">Bytes allocated during the measured iterations.</param>
/// <param name="Throughput">Operations completed per second.</param>
/// <param name="Unit">Caller-defined throughput unit.</param>
/// <param name="Gen0Collections">Generation-0 collections observed during the measured iterations. Reported, not gated.</param>
/// <param name="WorkingSetBytes">Process working set after the measured iterations. Reported, not gated.</param>
public sealed record BudgetSample(
    string Probe,
    string Parameters,
    TimeSpan Elapsed,
    long AllocatedBytes,
    double Throughput,
    string Unit,
    int Gen0Collections,
    long WorkingSetBytes)
{
    /// <summary>Projects this sample onto the shared highlight row.</summary>
    /// <param name="gate">Ceiling headroom or profile error text.</param>
    /// <returns>The highlight row.</returns>
    public HighlightRow ToHighlight(string gate) =>
        new(Probe, Parameters, Elapsed, AllocatedBytes, Throughput, Unit, gate, Gen0Collections, WorkingSetBytes);
}
