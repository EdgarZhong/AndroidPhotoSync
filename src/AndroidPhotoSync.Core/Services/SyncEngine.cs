using AndroidPhotoSync.Core.Abstractions;
using AndroidPhotoSync.Core.Models;

namespace AndroidPhotoSync.Core.Services;

public sealed class SyncEngine
{
    private readonly IRemoteFileProvider _remoteFileProvider;
    private readonly ILocalFileProvider _localFileProvider;
    private readonly ICheckpointStore _checkpointStore;
    private readonly ILogWriter _logWriter;

    public SyncEngine(
        IRemoteFileProvider remoteFileProvider,
        ILocalFileProvider localFileProvider,
        ICheckpointStore checkpointStore,
        ILogWriter logWriter)
    {
        _remoteFileProvider = remoteFileProvider;
        _localFileProvider = localFileProvider;
        _checkpointStore = checkpointStore;
        _logWriter = logWriter;
    }

    public async Task<SyncResult> ExecuteAsync(SyncOptions options, CancellationToken cancellationToken)
    {
        var checkpoint = await _checkpointStore.LoadAsync(options.CheckpointKey, cancellationToken);
        var remoteFiles = await _remoteFileProvider.ListFilesAsync(options.RemoteRoot, options.Recursive, cancellationToken);

        if (options.AllowedExtensions is not null && options.AllowedExtensions.Length > 0)
        {
            var allowed = new HashSet<string>(options.AllowedExtensions, StringComparer.OrdinalIgnoreCase);
            // Filter files based on extension
            var filtered = remoteFiles.Where(f => 
            {
                var ext = Path.GetExtension(f.RelativePath);
                if (string.IsNullOrEmpty(ext)) return false;
                return allowed.Contains(ext);
            }).ToList();
            remoteFiles = filtered;
        }

        var localFiles = await _localFileProvider.ListFilesAsync(options.LocalRoot, cancellationToken);
        var nowUtc = DateTimeOffset.UtcNow;
        var plan = SyncPlanner.CreatePlan(remoteFiles, localFiles, checkpoint, nowUtc);

        await _logWriter.WriteAsync(new SyncLogEntry
        {
            TimestampUtc = nowUtc,
            Level = "Info",
            Message = "SyncStarted",
            Details = $"Total={plan.TotalFiles}, Pending={plan.PendingCopies}"
        }, cancellationToken);

        var copied = 0;
        var skipped = 0;
        var conflicts = 0;
        var errors = 0;
        var isCancelled = false;

        foreach (var action in plan.Actions)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                isCancelled = true;
                break;
            }

            if (action.ActionType == SyncActionType.Skip)
            {
                skipped++;
                await _logWriter.WriteAsync(new SyncLogEntry
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Level = "Info",
                    Message = "SyncSkipped",
                    RelativePath = action.RelativePath,
                    Details = action.Reason
                }, cancellationToken);
                continue;
            }

            var localTargetPath = PathHelper.CombineToLocalPath(options.LocalRoot, action.TargetRelativePath);
            var localTargetDirectory = Path.GetDirectoryName(localTargetPath);
            if (!string.IsNullOrWhiteSpace(localTargetDirectory))
            {
                await _localFileProvider.EnsureDirectoryAsync(localTargetDirectory);
            }

            try
            {
                var remotePath = $"{options.RemoteRoot.TrimEnd('/', '\\')}/{action.RelativePath}";
                await _remoteFileProvider.TransferFileToLocalAsync(remotePath, localTargetPath, options.OverwriteExisting, cancellationToken);
                checkpoint.CompletedRelativePaths.Add(action.RelativePath);
                checkpoint = new SyncCheckpoint
                {
                    CompletedRelativePaths = checkpoint.CompletedRelativePaths,
                    UpdatedAtUtc = DateTimeOffset.UtcNow
                };
                await _checkpointStore.SaveAsync(options.CheckpointKey, checkpoint, cancellationToken);

                if (action.ActionType == SyncActionType.CopyWithRename)
                {
                    conflicts++;
                }
                else
                {
                    copied++;
                }

                await _logWriter.WriteAsync(new SyncLogEntry
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Level = "Info",
                    Message = "SyncCopied",
                    RelativePath = action.RelativePath,
                    Details = action.ActionType == SyncActionType.CopyWithRename ? "ConflictRename" : "Copied"
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                errors++;
                await _logWriter.WriteAsync(new SyncLogEntry
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    Level = "Error",
                    Message = "SyncFailed",
                    RelativePath = action.RelativePath,
                    Details = ex.Message
                }, cancellationToken);
            }
        }

        await _logWriter.WriteAsync(new SyncLogEntry
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Level = "Info",
            Message = "SyncCompleted",
            Details = $"Copied={copied}, Skipped={skipped}, Conflicts={conflicts}, Errors={errors}, Cancelled={isCancelled}"
        }, cancellationToken);

        return new SyncResult
        {
            TotalFiles = plan.TotalFiles,
            CopiedFiles = copied,
            SkippedFiles = skipped,
            ConflictCopies = conflicts,
            ErrorFiles = errors,
            IsCancelled = isCancelled
        };
    }
}
