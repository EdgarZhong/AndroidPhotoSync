using AndroidPhotoSync.Core.Models;

namespace AndroidPhotoSync.Core.Services;

public static class SyncPlanner
{
    public static SyncPlan CreatePlan(
        IReadOnlyList<FileEntry> remoteFiles,
        IReadOnlyList<FileEntry> localFiles,
        SyncCheckpoint checkpoint,
        DateTimeOffset nowUtc)
    {
        var localMap = localFiles
            .Where(item => !item.IsDirectory)
            .ToDictionary(item => PathHelper.NormalizeRelativePath(item.RelativePath), StringComparer.OrdinalIgnoreCase);

        var actions = new List<SyncAction>();
        var totalFiles = 0;
        var pendingCopies = 0;

        foreach (var remoteFile in remoteFiles.Where(item => !item.IsDirectory))
        {
            var relativePath = PathHelper.NormalizeRelativePath(remoteFile.RelativePath);
            totalFiles++;

            if (checkpoint.CompletedRelativePaths.Contains(relativePath))
            {
                actions.Add(new SyncAction
                {
                    RelativePath = relativePath,
                    TargetRelativePath = relativePath,
                    ActionType = SyncActionType.Skip,
                    Reason = "Checkpoint"
                });
                continue;
            }

            if (!localMap.TryGetValue(relativePath, out var localFile))
            {
                actions.Add(new SyncAction
                {
                    RelativePath = relativePath,
                    TargetRelativePath = relativePath,
                    ActionType = SyncActionType.Copy
                });
                pendingCopies++;
                continue;
            }

            if (IsSameFile(remoteFile.Fingerprint, localFile.Fingerprint))
            {
                actions.Add(new SyncAction
                {
                    RelativePath = relativePath,
                    TargetRelativePath = relativePath,
                    ActionType = SyncActionType.Skip,
                    Reason = "Identical"
                });
                continue;
            }

            var conflictTarget = PathHelper.EnsureUniqueConflictName(relativePath, nowUtc);
            actions.Add(new SyncAction
            {
                RelativePath = relativePath,
                TargetRelativePath = conflictTarget,
                ActionType = SyncActionType.CopyWithRename,
                Reason = "Conflict"
            });
            pendingCopies++;
        }

        return new SyncPlan
        {
            Actions = actions,
            TotalFiles = totalFiles,
            PendingCopies = pendingCopies
        };
    }

    private static bool IsSameFile(FileFingerprint? remoteFingerprint, FileFingerprint? localFingerprint)
    {
        if (remoteFingerprint is null || localFingerprint is null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(remoteFingerprint.Sha256) &&
            !string.IsNullOrWhiteSpace(localFingerprint.Sha256))
        {
            return string.Equals(remoteFingerprint.Sha256, localFingerprint.Sha256, StringComparison.OrdinalIgnoreCase);
        }

        if (remoteFingerprint.SizeBytes.HasValue && localFingerprint.SizeBytes.HasValue &&
            remoteFingerprint.ModifiedTimeUtc.HasValue && localFingerprint.ModifiedTimeUtc.HasValue)
        {
            return remoteFingerprint.SizeBytes.Value == localFingerprint.SizeBytes.Value &&
                   remoteFingerprint.ModifiedTimeUtc.Value == localFingerprint.ModifiedTimeUtc.Value;
        }

        if (remoteFingerprint.SizeBytes.HasValue && localFingerprint.SizeBytes.HasValue)
        {
            return remoteFingerprint.SizeBytes.Value == localFingerprint.SizeBytes.Value;
        }

        return false;
    }
}
