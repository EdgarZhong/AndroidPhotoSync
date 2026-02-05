namespace AndroidPhotoSync.Core.Models;

public sealed class SyncResult
{
    public required int TotalFiles { get; init; }

    public required int CopiedFiles { get; init; }

    public required int SkippedFiles { get; init; }

    public required int ConflictCopies { get; init; }

    public required int ErrorFiles { get; init; }

    public required bool IsCancelled { get; init; }
}
