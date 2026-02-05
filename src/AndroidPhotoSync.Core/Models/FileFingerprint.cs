namespace AndroidPhotoSync.Core.Models;

/// <summary>
/// 文件指纹信息，用于判断文件是否一致。
/// </summary>
public sealed class FileFingerprint
{
    /// <summary>
    /// 文件大小（字节）。
    /// </summary>
    public long? SizeBytes { get; init; }

    /// <summary>
    /// 修改时间（UTC）。
    /// </summary>
    public DateTimeOffset? ModifiedTimeUtc { get; init; }

    /// <summary>
    /// SHA256 哈希（可选）。
    /// </summary>
    public string? Sha256 { get; init; }
}
