using System.Text;

namespace Novolis.Testing.Timeline;

/// <summary>Renders a timeline as a Markdown story.</summary>
public static class TimelineMarkdownRenderer
{
    /// <summary>Renders a timeline with optional collection-index link.</summary>
    public static string Render(
        TimelineManifest manifest,
        string? indexHref = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(indexHref))
        {
            text.AppendLine($"[All timelines]({indexHref})");
            text.AppendLine();
        }

        text.AppendLine($"# {TimelineTitle.Display(manifest.Title)}");
        text.AppendLine();
        text.AppendLine(
            $"{TimelineOutline.SectionCount(manifest.Steps)} sections · {manifest.Steps.Count} frames");
        text.AppendLine();
        text.AppendLine(
            "The scenario is this whole test. Sections nest, and each frame preserves its narration and image.");
        text.AppendLine();

        foreach (var operation in TimelineOutline.Build(manifest.Steps))
        {
            if (operation.Action == "open"
                && !string.IsNullOrWhiteSpace(operation.Heading))
            {
                text.Append(Indent(operation.Depth - 1))
                    .Append("- **")
                    .Append(operation.Heading)
                    .AppendLine("**")
                    .AppendLine();
            }
            else if (operation.Action == "step"
                && operation.Step is { } step)
            {
                text.Append(Indent(operation.Depth))
                    .Append("1. ")
                    .AppendLine(step.Name)
                    .AppendLine();
                if (!string.IsNullOrWhiteSpace(step.Narration))
                {
                    text.Append(Indent(operation.Depth + 1))
                        .AppendLine(step.Narration)
                        .AppendLine();
                }

                if (!string.IsNullOrWhiteSpace(step.FrameFile))
                {
                    text.Append(Indent(operation.Depth + 1))
                        .Append("![")
                        .Append(step.Name)
                        .Append("](")
                        .Append(step.FrameFile.Replace('\\', '/'))
                        .AppendLine(")")
                        .AppendLine();
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(manifest.RawCaptureFile))
        {
            text.AppendLine("## Raw capture");
            text.AppendLine();
            text.AppendLine(
                $"[{manifest.RawCaptureFile}]({manifest.RawCaptureFile})");
            text.AppendLine();
        }

        return text.ToString();
    }

    private static string Indent(int depth) =>
        depth <= 0 ? string.Empty : new string(' ', depth * 2);
}
