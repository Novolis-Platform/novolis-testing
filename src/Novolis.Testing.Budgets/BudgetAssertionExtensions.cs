using System.Globalization;
using TUnit.Core;

namespace Novolis.Testing.Budgets;

/// <summary>Prints a budget sample and fails the current TUnit test when a ceiling breaks.</summary>
public static class BudgetAssertionExtensions
{
    /// <summary>Writes the highlight row, then asserts every ceiling on <paramref name="budget"/>.</summary>
    /// <param name="sample">Measured sample.</param>
    /// <param name="budget">Ceilings. Unset values are skipped.</param>
    /// <returns>A task that completes when the assertions pass.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sample"/> or <paramref name="budget"/> is null.</exception>
    public static async Task AssertWithin(this BudgetSample sample, Budget budget)
    {
        ArgumentNullException.ThrowIfNull(sample);
        ArgumentNullException.ThrowIfNull(budget);

        var breaches = new List<string>();
        if (budget.MaxElapsed is { } maxElapsed && sample.Elapsed > maxElapsed)
            breaches.Add($"elapsed {Highlight.FormatElapsed(sample.Elapsed)} > {Highlight.FormatElapsed(maxElapsed)}");

        if (budget.MaxAllocatedBytes is { } maxAllocated && sample.AllocatedBytes > maxAllocated)
        {
            breaches.Add(
                $"allocated {sample.AllocatedBytes.ToString("N0", CultureInfo.InvariantCulture)} B > {maxAllocated.ToString("N0", CultureInfo.InvariantCulture)} B");
        }

        if (budget.MinThroughput is { } minThroughput && sample.Throughput < minThroughput)
        {
            breaches.Add(
                $"throughput {sample.Throughput.ToString("0.###", CultureInfo.InvariantCulture)} {sample.Unit} < {minThroughput.ToString("0.###", CultureInfo.InvariantCulture)} {sample.Unit}");
        }

        var gate = breaches.Count == 0
            ? DescribeHeadroom(sample, budget)
            : string.Join("; ", breaches);
        Highlight.Write([sample.ToHighlight(gate)]);
        await Assert.That(string.Join("; ", breaches)).IsEqualTo(string.Empty);
    }

    private static string DescribeHeadroom(BudgetSample sample, Budget budget)
    {
        var parts = new List<string>();
        if (budget.MaxElapsed is { } maxElapsed && maxElapsed > TimeSpan.Zero)
        {
            var remaining = 1d - (sample.Elapsed.TotalSeconds / maxElapsed.TotalSeconds);
            parts.Add($"elapsed headroom {remaining.ToString("0%", CultureInfo.InvariantCulture)}");
        }

        if (budget.MaxAllocatedBytes is > 0 and var maxAllocated)
        {
            var remaining = 1d - (sample.AllocatedBytes / (double)maxAllocated);
            parts.Add($"allocated headroom {remaining.ToString("0%", CultureInfo.InvariantCulture)}");
        }

        if (budget.MinThroughput is > 0 and var minThroughput)
        {
            var multiple = sample.Throughput / minThroughput;
            parts.Add($"throughput {multiple.ToString("0.0", CultureInfo.InvariantCulture)}x floor");
        }

        return parts.Count == 0 ? "no ceilings" : string.Join("; ", parts);
    }
}
