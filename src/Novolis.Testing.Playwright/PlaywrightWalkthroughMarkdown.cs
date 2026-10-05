using System.Text;

namespace Novolis.Testing.Playwright;

/// <summary>Writes a walkthrough as Markdown — a separate file for editors and LLMs.</summary>
public static class PlaywrightWalkthroughMarkdown
{
    /// <summary>Renders one scenario as a readable Markdown document with frames.</summary>
    /// <param name="manifest">Walkthrough written by <see cref="PlaywrightSession"/>.</param>
    /// <param name="indexHref">Optional relative link back to the collection index.</param>
    /// <returns>Markdown text.</returns>
    public static string Render(PlaywrightWalkthroughManifest manifest, string? indexHref = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var title = PlaywrightWalkthroughTitle.Display(manifest.Title);
        var sections = PlaywrightWalkthroughOutline.SectionCount(manifest.Steps);
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(indexHref))
        {
            text.AppendLine($"[All walkthroughs]({indexHref})");
            text.AppendLine();
        }

        text.AppendLine($"# {title}");
        text.AppendLine();
        text.AppendLine($"{sections} sections · {manifest.Steps.Count} steps");
        text.AppendLine();
        text.AppendLine("The scenario is this whole test. A section is a heading plus indent. Sub-sections nest. A sectionless sub-step is only indent.");
        text.AppendLine();

        foreach (var op in PlaywrightWalkthroughOutline.Build(manifest.Steps))
        {
            if (op.Action == "open" && !string.IsNullOrWhiteSpace(op.Heading))
            {
                text.Append(Indent(op.Depth - 1));
                text.AppendLine($"- **{op.Heading}**");
                text.AppendLine();
            }
            else if (op.Action == "step" && op.Step is { } step)
            {
                text.Append(Indent(op.Depth));
                text.AppendLine($"1. {step.Name}");
                text.AppendLine();
                if (!string.IsNullOrWhiteSpace(step.Narration))
                {
                    text.Append(Indent(op.Depth + 1));
                    text.AppendLine(step.Narration);
                    text.AppendLine();
                }

                if (!string.IsNullOrWhiteSpace(step.FrameFile))
                {
                    text.Append(Indent(op.Depth + 1));
                    text.AppendLine($"![{step.Name}]({step.FrameFile.Replace('\\', '/')})");
                    text.AppendLine();
                }
            }
        }

        return text.ToString();
    }

    private static string Indent(int depth) =>
        depth <= 0 ? string.Empty : new string(' ', depth * 2);
}
