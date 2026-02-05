using AndroidPhotoSync.Core.Models;

namespace AndroidPhotoSync.Core.Abstractions;

/// <summary>
/// 本地文件访问接口，用于屏蔽文件系统细节，便于测试与扩展。
/// </summary>
public interface ILocalFileProvider
{
    /// <summary>
    /// 列出本地目录下的所有文件条目，返回相对于根目录的路径。
    /// </summary>
    /// <param name="localRoot">本地根目录路径。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>本地文件条目集合。</returns>
    Task<IReadOnlyList<FileEntry>> ListFilesAsync(string localRoot, CancellationToken cancellationToken);

    /// <summary>
    /// 获取本地文件条目，若不存在则返回 null。
    /// </summary>
    /// <param name="localPath">本地文件完整路径。</param>
    /// <returns>文件条目或 null。</returns>
    FileEntry? TryGetFileEntry(string localPath);

    /// <summary>
    /// 确保目录存在，如不存在则创建。
    /// </summary>
    /// <param name="directoryPath">目录路径。</param>
    Task EnsureDirectoryAsync(string directoryPath);
}
