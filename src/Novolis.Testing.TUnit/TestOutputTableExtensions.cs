using ConsoleTableExt;
using TUnit.Core;

namespace Novolis.Testing.TUnit;

/// <summary>Renders tabular data to TUnit test output.</summary>
public static class TestOutputTableExtensions
{
    /// <summary>Writes a console-style table for the given rows.</summary>
    /// <typeparam name="T">Row type.</typeparam>
    /// <param name="outputHelper">TUnit test context.</param>
    /// <param name="source">Rows to display.</param>
    /// <param name="format">Table format.</param>
    public static void WriteTable<T>(this TestContext? outputHelper, IEnumerable<T> source, ConsoleTableBuilderFormat format = ConsoleTableBuilderFormat.Minimal) =>
        outputHelper?.OutputWriter.WriteLine(ConsoleTableBuilder
            .From(source.Cast<object>().ToList())
            .WithFormat(format)
            .Export()
            .ToString());
}
