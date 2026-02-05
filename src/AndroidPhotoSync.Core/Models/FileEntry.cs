namespace AndroidPhotoSync.Core.Models;

/// <summary>
/// 文件条目，描述相对路径与指纹信息。
/// </summary>
public sealed class FileEntry
{
    /// <summary>
    /// 相对路径，统一使用正斜杠分隔。
    /// </summary>
    public required string RelativePath { get; init; }

    /// <summary>
    /// 是否为目录。
    /// </summary>
    public bool IsDirectory { get; init; }

    /// <summary>
    /// 文件指纹（目录可为空）。
    /// </summary>
    public FileFingerprint? Fingerprint { get; init; }
}
