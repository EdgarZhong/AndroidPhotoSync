namespace AndroidPhotoSync.Core.Models;

public sealed class SyncCheckpoint
{
    public HashSet<string> CompletedRelativePaths { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    public DateTimeOffset? UpdatedAtUtc { get; init; }

    public static SyncCheckpoint Empty()
    {
        return new SyncCheckpoint
        {
            CompletedRelativePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            UpdatedAtUtc = null
        };
    }
}
