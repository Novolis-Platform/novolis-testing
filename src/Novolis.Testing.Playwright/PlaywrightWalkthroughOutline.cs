namespace Novolis.Testing.Playwright;

/// <summary>Turns a flat step list into an infinitely nestable section outline.</summary>
public static class PlaywrightWalkthroughOutline
{
    /// <summary>Section headings from the root, empty string meaning indent only.</summary>
    /// <param name="step">Captured step.</param>
    /// <returns>Root-first path.</returns>
    public static IReadOnlyList<string> PathOf(PlaywrightWalkthroughStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        if (step.Sections is { Count: > 0 })
        {
            return step.Sections;
        }

        var path = new List<string>();
        if (!string.IsNullOrWhiteSpace(step.Part) &&
            !step.Part.Equals("Scenario", StringComparison.Ordinal))
        {
            path.Add(step.Part);
        }

        if (!string.IsNullOrWhiteSpace(step.Flow))
        {
            var anonymous = Math.Max(0, step.Depth - 1);
            for (var index = 0; index < anonymous; index++)
            {
                path.Add(string.Empty);
            }

            path.Add(step.Flow);
        }

        return path;
    }

    /// <summary>Named top-level sections in a scenario.</summary>
    /// <param name="steps">Captured steps.</param>
    /// <returns>Count of distinct first headings.</returns>
    public static int SectionCount(IEnumerable<PlaywrightWalkthroughStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        return steps
            .Select(PathOf)
            .Select(path => path.FirstOrDefault(heading => !string.IsNullOrWhiteSpace(heading)))
            .Where(heading => !string.IsNullOrWhiteSpace(heading))
            .Distinct(StringComparer.Ordinal)
            .Count();
    }

    /// <summary>Walks opens, steps, and closes so a renderer can nest lists.</summary>
    /// <param name="steps">Captured steps in order.</param>
    /// <returns>Outline operations.</returns>
    public static IReadOnlyList<PlaywrightWalkthroughOutlineOp> Build(
        IReadOnlyList<PlaywrightWalkthroughStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        var ops = new List<PlaywrightWalkthroughOutlineOp>();
        var current = new List<string>();
        foreach (var step in steps)
        {
            var target = PathOf(step).ToList();
            var shared = SharedPrefix(current, target);
            while (current.Count > shared)
            {
                ops.Add(new PlaywrightWalkthroughOutlineOp(
                    "close",
                    current.Count,
                    current[^1],
                    string.Empty,
                    null));
                current.RemoveAt(current.Count - 1);
            }

            while (current.Count < target.Count)
            {
                var heading = target[current.Count];
                current.Add(heading);
                ops.Add(new PlaywrightWalkthroughOutlineOp(
                    "open",
                    current.Count,
                    heading,
                    current.Count == target.Count ? step.Kind ?? string.Empty : string.Empty,
                    null));
            }

            ops.Add(new PlaywrightWalkthroughOutlineOp(
                "step",
                current.Count,
                string.Empty,
                step.Kind ?? string.Empty,
                step));
        }

        while (current.Count > 0)
        {
            ops.Add(new PlaywrightWalkthroughOutlineOp(
                "close",
                current.Count,
                current[^1],
                string.Empty,
                null));
            current.RemoveAt(current.Count - 1);
        }

        return ops;
    }

    private static int SharedPrefix(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        var count = Math.Min(left.Count, right.Count);
        var shared = 0;
        while (shared < count &&
               string.Equals(left[shared], right[shared], StringComparison.Ordinal))
        {
            shared++;
        }

        return shared;
    }
}
