using Novolis.Testing.Budgets;

namespace Novolis.Testing.Unit;

public sealed class BudgetProbeTests
{
    [Test]
    public async Task Measure_counts_warmup_separately_and_reports_throughput()
    {
        var calls = 0;
        var sample = BudgetProbe.Measure(
            new BudgetRun("noop", Iterations: 4, Warmup: 2, OperationsPerIteration: 10, Unit: "ops/s", Parameters: "fixed"),
            () => calls++);

        await Assert.That(calls).IsEqualTo(6);
        await Assert.That(sample.Probe).IsEqualTo("noop");
        await Assert.That(sample.Unit).IsEqualTo("ops/s");
        await Assert.That(sample.Throughput).IsGreaterThan(0);
        await sample.AssertWithin(new Budget(MaxElapsed: TimeSpan.FromSeconds(5), MinThroughput: 1));
        var highlight = Highlight.Format([sample.ToHighlight("elapsed headroom")]);
        await Assert.That(highlight).Contains("Gen0");
        await Assert.That(highlight).Contains("WorkingSet");
    }

    [Test]
    public async Task Prepare_stays_outside_the_measured_allocation()
    {
        var prepares = 0;
        var sample = BudgetProbe.Measure(
            new BudgetRun(
                "prepare",
                Iterations: 3,
                Warmup: 1,
                Prepare: _ =>
                {
                    prepares++;
                    var buffer = new byte[64 * 1024];
                    GC.KeepAlive(buffer);
                    return ValueTask.CompletedTask;
                }),
            () =>
            {
                var buffer = new byte[32];
                GC.KeepAlive(buffer);
            });

        await Assert.That(prepares).IsEqualTo(4);
        await Assert.That(sample.AllocatedBytes).IsLessThan(32 * 1024);
    }

    [Test]
    public async Task Measure_rejects_a_broken_allocation_ceiling()
    {
        var sample = BudgetProbe.Measure(
            new BudgetRun("allocate", Iterations: 1, Warmup: 0),
            () => _ = new byte[8_192]);

        await Assert.That(async () => await sample.AssertWithin(new Budget(MaxAllocatedBytes: 1)))
            .Throws<Exception>();
    }

    [Test]
    public async Task MeasureAsync_requires_process_allocations()
    {
        await Assert.That(async () => await BudgetProbe.MeasureAsync(
                new BudgetRun("async", Iterations: 1, Warmup: 0),
                _ => Task.CompletedTask))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task MeasureAsync_records_process_allocations()
    {
        var sample = await BudgetProbe.MeasureAsync(
            new BudgetRun(
                "async-alloc",
                Iterations: 1,
                Warmup: 0,
                Allocations: AllocationScope.Process,
                Unit: "ops/s"),
            _ =>
            {
                var buffer = new byte[4_096];
                GC.KeepAlive(buffer);
                return Task.CompletedTask;
            });

        await Assert.That(sample.AllocatedBytes).IsGreaterThan(0);
        await sample.AssertWithin(new Budget(MaxElapsed: TimeSpan.FromSeconds(5), MaxAllocatedBytes: 50_000_000));
    }

    [Test]
    public async Task Read_maps_benchmarkdotnet_json_onto_highlight_rows()
    {
        const string json = """
            {
              "Benchmarks": [
                {
                  "Method": "TimeToFirstSlice",
                  "Parameters": "SizeMegabytes=100",
                  "Statistics": { "Mean": 20000000, "StandardError": 100000 },
                  "Metrics": [ { "Name": "Allocated", "Unit": "B", "Value": 4096 } ]
                }
              ]
            }
            """;

        var rows = BenchmarkReport.Read(json);

        await Assert.That(rows.Count).IsEqualTo(1);
        await Assert.That(rows[0].Probe).IsEqualTo("TimeToFirstSlice");
        await Assert.That(rows[0].Parameters).IsEqualTo("SizeMegabytes=100");
        await Assert.That(rows[0].AllocatedBytes).IsEqualTo(4096);
        await Assert.That(Math.Abs(rows[0].Elapsed.TotalMilliseconds - 20)).IsLessThan(0.01);
        await Assert.That(Math.Abs(rows[0].Throughput - 50)).IsLessThan(0.01);
        await Assert.That(rows[0].Unit).IsEqualTo("ops/s");
    }

    [Test]
    public async Task Read_maps_a_benchmarkdotnet_0_15_full_report()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "ndjson-time-to-first-slice-report.json");
        var rows = BenchmarkReport.Read(File.ReadAllText(path));

        await Assert.That(rows.Count).IsEqualTo(1);
        await Assert.That(rows[0].Probe).IsEqualTo("TimeToFirstSliceAsync");
        await Assert.That(rows[0].Parameters).IsEqualTo("SizeMegabytes=100&RecordShape=short&NewlineStyle=LF");
        await Assert.That(rows[0].AllocatedBytes).IsEqualTo(110_473);
        await Assert.That(rows[0].Gen0Collections).IsEqualTo(3);
        await Assert.That(rows[0].WorkingSetBytes).IsEqualTo(0);
        await Assert.That(Math.Abs(rows[0].Elapsed.TotalMilliseconds - 1.503851721)).IsLessThan(0.001);
        await Assert.That(rows[0].Unit).IsEqualTo("ops/s");
    }

    [Test]
    public async Task Read_maps_allocated_bytes_from_a_nested_metric_descriptor()
    {
        const string json = """
            {
              "Benchmarks": [
                {
                  "Method": "TimeToFirstSliceAsync",
                  "Parameters": "SizeMegabytes=100&RecordShape=short&NewlineStyle=LF",
                  "Statistics": { "Mean": 1503851.7213255495, "StandardError": 18319.333358360036 },
                  "Metrics": [
                    {
                      "Value": 110473,
                      "Descriptor": { "Id": "Allocated Memory", "DisplayName": "Allocated", "Unit": "B" }
                    }
                  ]
                }
              ]
            }
            """;

        var rows = BenchmarkReport.Read(json);

        await Assert.That(rows[0].AllocatedBytes).IsEqualTo(110_473);
        await Assert.That(rows[0].Gen0Collections).IsEqualTo(0);
    }

    [Test]
    public async Task TryRead_missing_file_returns_false()
    {
        var found = BenchmarkReport.TryRead(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.json"), out var rows);

        await Assert.That(found).IsFalse();
        await Assert.That(rows.Count).IsEqualTo(0);
    }

    [Test]
    public async Task FindLatest_returns_null_when_the_directory_is_missing()
    {
        var path = BenchmarkReport.FindLatest(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));

        await Assert.That(path).IsNull();
    }
}
