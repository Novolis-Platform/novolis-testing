using System.Globalization;
using System.Text;
using TUnit.Core;

namespace Novolis.Testing.Budgets;

/// <summary>Formats highlight rows for test output.</summary>
public static class Highlight
{
    /// <summary>Writes rows to the current TUnit output, when a test is running.</summary>
    /// <param name="rows">Rows to print.</param>
    public static void Write(IEnumerable<HighlightRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        TestContext.Current?.OutputWriter.WriteLine(Format(rows));
    }

    /// <summary>Renders a text table of highlight rows.</summary>
    /// <param name="rows">Rows to render.</param>
    /// <returns>A table with probe, parameters, elapsed, allocated, gen-0, working set, throughput, and gate columns.</returns>
    public static string Format(IEnumerable<HighlightRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var builder = new StringBuilder();
        builder.AppendLine("Probe | Parameters | Elapsed | Allocated | Gen0 | WorkingSet | Throughput | Gate");
        foreach (var row in rows)
        {
            builder.Append(row.Probe);
            builder.Append(" | ");
            builder.Append(row.Parameters);
            builder.Append(" | ");
            builder.Append(FormatElapsed(row.Elapsed));
            builder.Append(" | ");
            builder.Append(row.AllocatedBytes.ToString("N0", CultureInfo.InvariantCulture));
            builder.Append(" B | ");
            builder.Append(row.Gen0Collections.ToString("N0", CultureInfo.InvariantCulture));
            builder.Append(" | ");
            builder.Append(row.WorkingSetBytes.ToString("N0", CultureInfo.InvariantCulture));
            builder.Append(" B | ");
            builder.Append(row.Throughput.ToString("0.###", CultureInfo.InvariantCulture));
            builder.Append(' ');
            builder.Append(row.Unit);
            builder.Append(" | ");
            builder.AppendLine(row.Gate);
        }

        return builder.ToString();
    }

    internal static string FormatElapsed(TimeSpan elapsed) =>
        elapsed.TotalSeconds >= 1
            ? elapsed.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture) + " s"
            : elapsed.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture) + " ms";
}
