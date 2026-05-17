using ConsoleTableExt;
using TUnit.Core;

namespace Novolis.Testing.TUnit;

public static class TestOutputTableExtensions
{
    public static void WriteTable<T>(this TestContext? outputHelper, IEnumerable<T> source, ConsoleTableBuilderFormat format = ConsoleTableBuilderFormat.Minimal) =>
        outputHelper?.OutputWriter.WriteLine(ConsoleTableBuilder
            .From(source.Cast<object>().ToList())
            .WithFormat(format)
            .Export()
            .ToString());
}
