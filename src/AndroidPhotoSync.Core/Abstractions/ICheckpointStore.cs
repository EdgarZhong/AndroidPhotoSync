using AndroidPhotoSync.Core.Models;

namespace AndroidPhotoSync.Core.Abstractions;

/// <summary>
/// 同步断点存储接口，用于保存与加载任务断点。
/// </summary>
public interface ICheckpointStore
{
    /// <summary>
    /// 加载断点数据。
    /// </summary>
    /// <param name="checkpointKey">断点唯一键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>断点数据。</returns>
    Task<SyncCheckpoint> LoadAsync(string checkpointKey, CancellationToken cancellationToken);

    /// <summary>
    /// 保存断点数据。
    /// </summary>
    /// <param name="checkpointKey">断点唯一键。</param>
    /// <param name="checkpoint">断点数据。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task SaveAsync(string checkpointKey, SyncCheckpoint checkpoint, CancellationToken cancellationToken);
}
