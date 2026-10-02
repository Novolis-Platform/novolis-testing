using System.Text.Json;

namespace Novolis.Testing.Budgets;

/// <summary>Reads a BenchmarkDotNet full JSON export into highlight rows.</summary>
public static class BenchmarkReport
{
    /// <summary>Finds the newest <c>*-report-full-compressed.json</c> under <paramref name="directory"/>.</summary>
    /// <param name="directory">Profile artifact directory.</param>
    /// <returns>The newest report path, or <see langword="null"/> when none exist.</returns>
    public static string? FindLatest(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        if (!Directory.Exists(directory))
            return null;

        FileInfo? newest = null;
        foreach (var path in Directory.EnumerateFiles(directory, "*-report-full-compressed.json", SearchOption.AllDirectories))
        {
            var info = new FileInfo(path);
            if (newest is null || info.LastWriteTimeUtc > newest.LastWriteTimeUtc)
                newest = info;
        }

        return newest?.FullName;
    }

    /// <summary>Reads highlight rows when <paramref name="path"/> exists and contains benchmarks.</summary>
    /// <param name="path">JSON report path.</param>
    /// <param name="rows">Parsed rows when the method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when at least one benchmark row was read.</returns>
    public static bool TryRead(string? path, out IReadOnlyList<HighlightRow> rows)
    {
        rows = [];
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return false;

        rows = Read(File.ReadAllText(path));
        return rows.Count > 0;
    }

    /// <summary>Parses BenchmarkDotNet JSON. Mean values are nanoseconds. Throughput is one operation per invocation.</summary>
    /// <param name="json">Full or full-compressed BenchmarkDotNet JSON.</param>
    /// <returns>One highlight row per benchmark entry.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is null.</exception>
    /// <exception cref="JsonException">The document is not a JSON object.</exception>
    public static IReadOnlyList<HighlightRow> Read(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object
            || !document.RootElement.TryGetProperty("Benchmarks", out var benchmarks)
            || benchmarks.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var rows = new List<HighlightRow>();
        foreach (var benchmark in benchmarks.EnumerateArray())
        {
            var probe = ReadString(benchmark, "Method");
            if (probe.Length == 0)
                probe = ReadString(benchmark, "DisplayInfo");

            var elapsed = TimeSpan.Zero;
            var error = TimeSpan.Zero;
            if (benchmark.TryGetProperty("Statistics", out var statistics) && statistics.ValueKind == JsonValueKind.Object)
            {
                if (statistics.TryGetProperty("Mean", out var mean) && mean.TryGetDouble(out var meanNs))
                    elapsed = FromNanoseconds(meanNs);

                if (statistics.TryGetProperty("StandardError", out var standardError) && standardError.TryGetDouble(out var errorNs))
                    error = FromNanoseconds(errorNs);
            }

            var throughput = elapsed.TotalSeconds <= 0
                ? double.PositiveInfinity
                : 1d / elapsed.TotalSeconds;
            rows.Add(new HighlightRow(
                probe,
                ReadParameters(benchmark),
                elapsed,
                ReadAllocated(benchmark),
                throughput,
                "ops/s",
                $"± {Highlight.FormatElapsed(error)}"));
        }

        return rows;
    }

    private static string ReadParameters(JsonElement benchmark)
    {
        if (!benchmark.TryGetProperty("Parameters", out var parameters))
            return string.Empty;

        return parameters.ValueKind switch
        {
            JsonValueKind.String => parameters.GetString() ?? string.Empty,
            JsonValueKind.Null => string.Empty,
            _ => parameters.GetRawText(),
        };
    }

    private static long ReadAllocated(JsonElement benchmark)
    {
        if (benchmark.TryGetProperty("Memory", out var memory)
            && memory.ValueKind == JsonValueKind.Object
            && memory.TryGetProperty("BytesAllocatedPerOperation", out var bytes)
            && bytes.TryGetInt64(out var allocated))
        {
            return allocated;
        }

        if (!benchmark.TryGetProperty("Metrics", out var metrics) || metrics.ValueKind != JsonValueKind.Array)
            return 0;

        foreach (var metric in metrics.EnumerateArray())
        {
            var name = ReadString(metric, "Name");
            if (name.Length == 0)
                name = ReadString(metric, "DisplayName");

            if (!name.Contains("Allocat", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!metric.TryGetProperty("Value", out var value) || !value.TryGetDouble(out var amount))
                continue;

            var unit = ReadString(metric, "Unit");
            if (unit.Equals("KB", StringComparison.OrdinalIgnoreCase))
                return (long)Math.Round(amount * 1024d);

            if (unit.Equals("MB", StringComparison.OrdinalIgnoreCase))
                return (long)Math.Round(amount * 1024d * 1024d);

            return (long)Math.Round(amount);
        }

        return 0;
    }

    private static string ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static TimeSpan FromNanoseconds(double nanoseconds) =>
        TimeSpan.FromTicks((long)Math.Round(nanoseconds / 100d, MidpointRounding.AwayFromZero));
}
