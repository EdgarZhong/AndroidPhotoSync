namespace AndroidPhotoSync.Core.Models;

public sealed class SyncLogEntry
{
    public required DateTimeOffset TimestampUtc { get; init; }

    public required string Level { get; init; }

    public required string Message { get; init; }

    public string? RelativePath { get; init; }

    public string? Details { get; init; }
}
