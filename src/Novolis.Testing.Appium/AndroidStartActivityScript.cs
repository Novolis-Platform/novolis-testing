namespace Novolis.Testing.Appium;

/// <summary>
/// Builds the argument dictionary for the <c>mobile:startActivity</c> extension command.
/// </summary>
public static class AndroidStartActivityScript
{
    /// <summary>
    /// Creates script arguments from a package and activity.
    /// </summary>
    /// <param name="appPackage">Android package name.</param>
    /// <param name="appActivity">Activity name, for example <c>.Settings</c>.</param>
    /// <returns>Arguments understood by UiAutomator2 <c>mobile:startActivity</c>.</returns>
    public static Dictionary<string, object> Arguments(string appPackage, string appActivity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appPackage);
        ArgumentException.ThrowIfNullOrWhiteSpace(appActivity);
        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["intent"] = $"{appPackage}/{appActivity}",
            ["appPackage"] = appPackage,
            ["appActivity"] = appActivity,
        };
    }
}
