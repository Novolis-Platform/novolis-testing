namespace Novolis.Testing.Timeline;

/// <summary>Resolves timeline assets to portable data URIs or relative paths.</summary>
public static class TimelineAssets
{
    /// <summary>Returns an embedded data URI when the asset exists; otherwise its relative path.</summary>
    public static string ToDataUriOrPath(string root, string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        var path = Path.Combine(
            root,
            relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(path)
            ? ToDataUri(root, relativePath)
            : relativePath.Replace('\\', '/');
    }

    /// <summary>Returns an embedded data URI for an existing timeline asset.</summary>
    public static string ToDataUri(string root, string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        var path = Path.Combine(
            root,
            relativePath.Replace('/', Path.DirectorySeparatorChar));
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
}
