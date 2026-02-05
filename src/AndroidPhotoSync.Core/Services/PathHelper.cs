namespace AndroidPhotoSync.Core.Services;

internal static class PathHelper
{
    public static string NormalizeRelativePath(string path)
    {
        return path.Replace('\\', '/');
    }

    public static string CombineToLocalPath(string localRoot, string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(localRoot, normalized);
    }

    public static string EnsureUniqueConflictName(string relativePath, DateTimeOffset nowUtc)
    {
        var extension = Path.GetExtension(relativePath);
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(relativePath);
        var directory = Path.GetDirectoryName(relativePath)?.Replace('\\', '/') ?? string.Empty;
        var stamp = nowUtc.ToString("yyyyMMddHHmmss");
        var conflictName = $"{fileNameWithoutExt}.conflict.{stamp}{extension}";
        return string.IsNullOrWhiteSpace(directory) ? conflictName : $"{directory}/{conflictName}";
    }
}
