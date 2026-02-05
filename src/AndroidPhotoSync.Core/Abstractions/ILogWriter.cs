using AndroidPhotoSync.Core.Models;

namespace AndroidPhotoSync.Core.Abstractions;

/// <summary>
/// 日志写入接口，用于输出结构化同步日志。
/// </summary>
public interface ILogWriter
{
    /// <summary>
    /// 写入单条日志。
    /// </summary>
    /// <param name="entry">日志条目。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task WriteAsync(SyncLogEntry entry, CancellationToken cancellationToken);
}
