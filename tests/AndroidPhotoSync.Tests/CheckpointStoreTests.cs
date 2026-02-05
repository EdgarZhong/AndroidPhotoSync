using AndroidPhotoSync.Core.Models;
using AndroidPhotoSync.Infrastructure;

namespace AndroidPhotoSync.Tests;

public class CheckpointStoreTests
{
    [Fact]
    public async Task SaveAndLoad_ShouldPersistCompletedPaths()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "AndroidPhotoSyncTests", Guid.NewGuid().ToString("N"));
        var store = new FileCheckpointStore(tempDir);
        var checkpointKey = "device-001";
        var checkpoint = new SyncCheckpoint
        {
            CompletedRelativePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "DCIM/IMG_0001.jpg",
                "DCIM/IMG_0002.jpg"
            },
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        await store.SaveAsync(checkpointKey, checkpoint, CancellationToken.None);
        var loaded = await store.LoadAsync(checkpointKey, CancellationToken.None);

        Assert.Equal(2, loaded.CompletedRelativePaths.Count);
        Assert.Contains("DCIM/IMG_0001.jpg", loaded.CompletedRelativePaths);
        Assert.Contains("DCIM/IMG_0002.jpg", loaded.CompletedRelativePaths);
    }
}
