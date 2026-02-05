namespace AndroidPhotoSync.Core.Models;

public sealed class SyncPlan
{
    public required IReadOnlyList<SyncAction> Actions { get; init; }

    public required int TotalFiles { get; init; }

    public required int PendingCopies { get; init; }
}
