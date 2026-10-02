namespace Novolis.Testing.Budgets;

/// <summary>One row printed for both a budget gate and a profile export.</summary>
/// <param name="Probe">Method or test name.</param>
/// <param name="Parameters">Case or BenchmarkDotNet parameter set.</param>
/// <param name="Elapsed">Measured elapsed time, or the profile mean.</param>
/// <param name="AllocatedBytes">Allocated bytes for one measured run.</param>
/// <param name="Throughput">Operations per second.</param>
/// <param name="Unit">Throughput unit.</param>
/// <param name="Gate">Ceiling headroom, or the profile error interval.</param>
public sealed record HighlightRow(
    string Probe,
    string Parameters,
    TimeSpan Elapsed,
    long AllocatedBytes,
    double Throughput,
    string Unit,
    string Gate);
