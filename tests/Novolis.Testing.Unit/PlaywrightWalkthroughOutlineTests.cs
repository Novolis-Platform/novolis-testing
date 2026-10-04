using Novolis.Testing.Playwright;

namespace Novolis.Testing.Unit;

public sealed class PlaywrightWalkthroughOutlineTests
{
    [Test]
    public async Task Build_nests_sections_and_sectionless_indents()
    {
        var ops = PlaywrightWalkthroughOutline.Build(
        [
            Step(1, "Sign in", ["Robin registers the month"]),
            Step(2, "Open My hours", ["Robin registers the month", "Set usual hours"]),
            Step(3, "Type 10:00–18:00", ["Robin registers the month", "Thursday · Changed", ""]),
        ]);

        await Assert.That(ops.Count(op => op.Action == "open")).IsEqualTo(4);
        await Assert.That(ops.Count(op => op.Action == "open" && op.Heading == "Set usual hours")).IsEqualTo(1);
        await Assert.That(ops.Count(op => op.Action == "open" && op.Heading == string.Empty)).IsEqualTo(1);
        await Assert.That(ops.Single(op => op.Action == "step" && op.Step!.Name == "Type 10:00–18:00").Depth)
            .IsEqualTo(3);
    }

    private static PlaywrightWalkthroughStep Step(int index, string name, IReadOnlyList<string> sections) =>
        new(
            index,
            sections[0],
            name,
            "Narration.",
            $"frames/{index:00}.png",
            DateTimeOffset.UnixEpoch,
            Depth: sections.Count,
            Flow: sections.LastOrDefault(heading => heading.Length > 0) ?? string.Empty,
            FlowPath: string.Join(" · ", sections.Where(heading => heading.Length > 0)),
            Kind: "",
            Sections: sections);
}
