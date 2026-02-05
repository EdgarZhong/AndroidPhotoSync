using System.Text.Json;
using AndroidPhotoSync.Core.Abstractions;
using AndroidPhotoSync.Core.Models;

namespace AndroidPhotoSync.Infrastructure;

public sealed class FileCheckpointStore : ICheckpointStore
{
    private readonly string _checkpointDirectory;
    private readonly JsonSerializerOptions _serializerOptions;

    public FileCheckpointStore(string checkpointDirectory)
    {
        _checkpointDirectory = checkpointDirectory;
        _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
    }

    public async Task<SyncCheckpoint> LoadAsync(string checkpointKey, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_checkpointDirectory);
        var filePath = GetCheckpointPath(checkpointKey);

        if (!File.Exists(filePath))
        {
            return SyncCheckpoint.Empty();
        }

        await using var stream = File.OpenRead(filePath);
        var checkpoint = await JsonSerializer.DeserializeAsync<SyncCheckpoint>(stream, _serializerOptions, cancellationToken);
        if (checkpoint is null)
        {
            return SyncCheckpoint.Empty();
        }

        if (checkpoint.CompletedRelativePaths.Count == 0)
        {
            checkpoint = new SyncCheckpoint
            {
                CompletedRelativePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                UpdatedAtUtc = checkpoint.UpdatedAtUtc
            };
        }

        return checkpoint;
    }

    public async Task SaveAsync(string checkpointKey, SyncCheckpoint checkpoint, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_checkpointDirectory);
        var filePath = GetCheckpointPath(checkpointKey);
        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, checkpoint, _serializerOptions, cancellationToken);
    }

    private string GetCheckpointPath(string checkpointKey)
    {
        var safeKey = string.Concat(checkpointKey.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
        return Path.Combine(_checkpointDirectory, $"{safeKey}.json");
    }
}
