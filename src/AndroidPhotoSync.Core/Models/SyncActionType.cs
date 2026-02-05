namespace AndroidPhotoSync.Core.Models;

public enum SyncActionType
{
    Skip = 0,
    Copy = 1,
    CopyWithRename = 2,
    Error = 3
}
