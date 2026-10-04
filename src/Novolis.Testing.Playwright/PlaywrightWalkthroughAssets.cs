namespace Novolis.Testing.Playwright;

/// <summary>Turns a captured frame path into a data URI for a portable HTML document.</summary>
public static class PlaywrightWalkthroughAssets
{
    /// <summary>
    /// Reads <paramref name="relativePath"/> under <paramref name="root"/> and returns a data URI.
    /// The renderer stays filesystem-free; this is the only compile step that opens a file.
    /// </summary>
    /// <param name="root">Recording folder that contains <c>frames/</c>.</param>
    /// <param name="relativePath">Path stored on the manifest, such as <c>frames/01.png</c>.</param>
    /// <returns>A <c>data:</c> URI.</returns>
    public static string ToDataUri(string root, string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var contentType = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".svg" => "image/svg+xml",
            ".webm" => "video/webm",
            _ => "application/octet-stream",
        };

        return $"data:{contentType};base64,{Convert.ToBase64String(File.ReadAllBytes(path))}";
    }

    /// <summary>
    /// Inlines the file when it exists under <paramref name="root"/>; otherwise keeps the relative path.
    /// </summary>
    /// <param name="root">Recording folder.</param>
    /// <param name="relativePath">Manifest asset path.</param>
    /// <returns>A data URI or the original relative path.</returns>
    public static string ToDataUriOrPath(string root, string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(path) ? ToDataUri(root, relativePath) : relativePath.Replace('\\', '/');
    }
}
