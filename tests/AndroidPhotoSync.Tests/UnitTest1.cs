using AndroidPhotoSync.Core.Models;
using AndroidPhotoSync.Core.Services;

namespace AndroidPhotoSync.Tests;

public class SyncPlannerTests
{
    [Fact]
    public void CreatePlan_ShouldSkipCheckpointAndIdentical()
    {
        var remoteFiles = new List<FileEntry>
        {
            new()
            {
                RelativePath = "DCIM/IMG_0001.jpg",
                IsDirectory = false,
                Fingerprint = new FileFingerprint
                {
                    SizeBytes = 100,
                    ModifiedTimeUtc = DateTimeOffset.Parse("2024-01-01T00:00:00Z")
                }
            },
            new()
            {
                RelativePath = "DCIM/IMG_0002.jpg",
                IsDirectory = false,
                Fingerprint = new FileFingerprint
                {
                    SizeBytes = 200,
                    ModifiedTimeUtc = DateTimeOffset.Parse("2024-01-02T00:00:00Z")
                }
            }
        };

        var localFiles = new List<FileEntry>
        {
            new()
            {
                RelativePath = "DCIM/IMG_0002.jpg",
                IsDirectory = false,
                Fingerprint = new FileFingerprint
                {
                    SizeBytes = 200,
                    ModifiedTimeUtc = DateTimeOffset.Parse("2024-01-02T00:00:00Z")
                }
            }
        };

        var checkpoint = new SyncCheckpoint
        {
            CompletedRelativePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "DCIM/IMG_0001.jpg"
            },
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        var plan = SyncPlanner.CreatePlan(remoteFiles, localFiles, checkpoint, DateTimeOffset.UtcNow);

        Assert.Equal(2, plan.TotalFiles);
        Assert.Equal(0, plan.PendingCopies);
        Assert.Equal(2, plan.Actions.Count);
        Assert.All(plan.Actions, action => Assert.Equal(SyncActionType.Skip, action.ActionType));
    }

    [Fact]
    public void CreatePlan_ShouldCreateConflictRename()
    {
        var remoteFiles = new List<FileEntry>
        {
            new()
            {
                RelativePath = "DCIM/IMG_0003.jpg",
                IsDirectory = false,
                Fingerprint = new FileFingerprint
                {
                    SizeBytes = 300,
                    ModifiedTimeUtc = DateTimeOffset.Parse("2024-01-03T00:00:00Z")
                }
            }
        };

        var localFiles = new List<FileEntry>
        {
            new()
            {
                RelativePath = "DCIM/IMG_0003.jpg",
                IsDirectory = false,
                Fingerprint = new FileFingerprint
                {
                    SizeBytes = 999,
                    ModifiedTimeUtc = DateTimeOffset.Parse("2024-01-03T00:00:00Z")
                }
            }
        };

        var checkpoint = SyncCheckpoint.Empty();

        var plan = SyncPlanner.CreatePlan(remoteFiles, localFiles, checkpoint, DateTimeOffset.Parse("2024-02-01T00:00:00Z"));

        var action = Assert.Single(plan.Actions);
        Assert.Equal(SyncActionType.CopyWithRename, action.ActionType);
        Assert.NotEqual(action.RelativePath, action.TargetRelativePath);
        Assert.Contains("conflict", action.TargetRelativePath);
    }
}
