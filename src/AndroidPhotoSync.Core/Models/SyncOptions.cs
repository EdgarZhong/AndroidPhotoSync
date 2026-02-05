namespace AndroidPhotoSync.Core.Models;

public sealed class SyncOptions
{
    public required string RemoteRoot { get; init; }

    public required string LocalRoot { get; init; }

    public required string CheckpointKey { get; init; }

    public bool OverwriteExisting { get; init; }

    public bool Recursive { get; init; } = true;

    public string[]? AllowedExtensions { get; init; }
}
