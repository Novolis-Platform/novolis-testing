using System.Text;

namespace Novolis.Testing.Timeline;

/// <summary>Turns test and type identifiers into readable timeline titles.</summary>
public static class TimelineTitle
{
    /// <summary>Replaces underscores with spaces.</summary>
    public static string FromTestName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var spaces = value.Replace('_', ' ').Trim();
        while (spaces.Contains("  ", StringComparison.Ordinal))
        {
            spaces = spaces.Replace("  ", " ", StringComparison.Ordinal);
        }

        return spaces;
    }

    /// <summary>Inserts spaces before capitals in a type name.</summary>
    public static string FromTypeName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var text = new StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            if (index > 0 && char.IsUpper(current))
            {
                var previous = value[index - 1];
                var nextIsLower = index + 1 < value.Length && char.IsLower(value[index + 1]);
                if (char.IsLower(previous) || (char.IsUpper(previous) && nextIsLower))
                {
                    text.Append(' ');
                }
            }

            text.Append(current);
        }

        return text.ToString();
    }

    /// <summary>Uses a stored title, or turns a test identifier into words.</summary>
    public static string Display(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "Timeline";
        }

        return title.Contains('_', StringComparison.Ordinal)
            ? FromTestName(title)
            : title.Trim();
    }
}
