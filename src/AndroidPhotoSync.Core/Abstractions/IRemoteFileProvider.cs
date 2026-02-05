using AndroidPhotoSync.Core.Models;

namespace AndroidPhotoSync.Core.Abstractions;

/// <summary>
/// 远端文件访问接口，用于抽象不同传输通道（例如 ADB）。
/// </summary>
public interface IRemoteFileProvider
{
    /// <summary>
    /// 列出远端目录下的所有文件条目，返回相对于根目录的路径。
    /// </summary>
    /// <param name="remoteRoot">远端根目录路径。</param>
    /// <param name="recursive">是否递归子目录。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>远端文件条目集合。</returns>
    Task<IReadOnlyList<FileEntry>> ListFilesAsync(string remoteRoot, bool recursive, CancellationToken cancellationToken);

    /// <summary>
    /// 将远端文件传输到本地指定路径。
    /// </summary>
    /// <param name="remotePath">远端文件完整路径。</param>
    /// <param name="localPath">本地目标文件完整路径。</param>
    /// <param name="overwrite">是否覆盖已存在的本地文件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task TransferFileToLocalAsync(
        string remotePath,
        string localPath,
        bool overwrite,
        CancellationToken cancellationToken);
}
