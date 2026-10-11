namespace Novolis.Testing.Timeline;

/// <summary>Builds a nestable outline from timeline frame section paths.</summary>
public static class TimelineOutline
{
    /// <summary>Returns the root-first section path for a frame.</summary>
    public static IReadOnlyList<string> PathOf(TimelineStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        if (step.Sections is { Count: > 0 })
        {
            return step.Sections;
        }

        var path = new List<string>();
        if (!string.IsNullOrWhiteSpace(step.Part)
            && !step.Part.Equals("Scenario", StringComparison.Ordinal))
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

    /// <summary>Counts distinct named top-level sections.</summary>
    public static int SectionCount(IEnumerable<TimelineStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        return steps
            .Select(PathOf)
            .Select(path => path.FirstOrDefault(heading => !string.IsNullOrWhiteSpace(heading)))
            .Where(heading => !string.IsNullOrWhiteSpace(heading))
            .Distinct(StringComparer.Ordinal)
            .Count();
    }

    /// <summary>Builds open, close, and frame operations for a renderer.</summary>
    public static IReadOnlyList<TimelineOutlineOp> Build(
        IReadOnlyList<TimelineStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        var operations = new List<TimelineOutlineOp>();
        var current = new List<string>();
        foreach (var step in steps)
        {
            var target = PathOf(step).ToList();
            var shared = SharedPrefix(current, target);
            while (current.Count > shared)
            {
                operations.Add(new TimelineOutlineOp(
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
                operations.Add(new TimelineOutlineOp(
                    "open",
                    current.Count,
                    heading,
                    current.Count == target.Count ? step.Kind : string.Empty,
                    null));
            }

            operations.Add(new TimelineOutlineOp(
                "step",
                current.Count,
                string.Empty,
                step.Kind,
                step));
        }

        while (current.Count > 0)
        {
            operations.Add(new TimelineOutlineOp(
                "close",
                current.Count,
                current[^1],
                string.Empty,
                null));
            current.RemoveAt(current.Count - 1);
        }

        return operations;
    }

    private static int SharedPrefix(
        IReadOnlyList<string> left,
        IReadOnlyList<string> right)
    {
        var count = Math.Min(left.Count, right.Count);
        var shared = 0;
        while (shared < count
            && string.Equals(left[shared], right[shared], StringComparison.Ordinal))
        {
            shared++;
        }

        return shared;
    }
}
