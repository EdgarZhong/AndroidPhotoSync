namespace AndroidPhotoSync.Core.Models;

public sealed class SyncAction
{
    public required string RelativePath { get; init; }

    public required string TargetRelativePath { get; init; }

    public required SyncActionType ActionType { get; init; }

    public string? Reason { get; init; }
}
