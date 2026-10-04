using System.Text;

namespace Novolis.Testing.Playwright;

/// <summary>Turns test and type identifiers into titles a person can read.</summary>
public static class PlaywrightWalkthroughTitle
{
    /// <summary>Replaces underscores with spaces.</summary>
    /// <param name="value">Test method name or folder segment.</param>
    /// <returns>A sentence-like title.</returns>
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
    /// <param name="value">Class name such as <c>HoursGameMonthScenarioTests</c>.</param>
    /// <returns>A spaced heading.</returns>
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

    /// <summary>Uses the stored title, or turns a test identifier into words.</summary>
    /// <param name="title">Manifest title, which may still be a method name.</param>
    /// <returns>Display title.</returns>
    public static string Display(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "Walkthrough";
        }

        return title.Contains('_', StringComparison.Ordinal)
            ? FromTestName(title)
            : title.Trim();
    }
}
