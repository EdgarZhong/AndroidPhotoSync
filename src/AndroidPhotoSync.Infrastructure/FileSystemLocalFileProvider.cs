using AndroidPhotoSync.Core.Abstractions;
using AndroidPhotoSync.Core.Models;

namespace AndroidPhotoSync.Infrastructure;

public sealed class FileSystemLocalFileProvider : ILocalFileProvider
{
    public Task<IReadOnlyList<FileEntry>> ListFilesAsync(string localRoot, CancellationToken cancellationToken)
    {
        var result = new List<FileEntry>();
        if (!Directory.Exists(localRoot))
        {
            return Task.FromResult<IReadOnlyList<FileEntry>>(result);
        }

        foreach (var filePath in Directory.EnumerateFiles(localRoot, "*", SearchOption.AllDirectories))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var fileInfo = new FileInfo(filePath);
            var relativePath = Path.GetRelativePath(localRoot, filePath);
            result.Add(new FileEntry
            {
                RelativePath = relativePath.Replace('\\', '/'),
                IsDirectory = false,
                Fingerprint = new FileFingerprint
                {
                    SizeBytes = fileInfo.Length,
                    ModifiedTimeUtc = fileInfo.LastWriteTimeUtc
                }
            });
        }

        return Task.FromResult<IReadOnlyList<FileEntry>>(result);
    }

    public FileEntry? TryGetFileEntry(string localPath)
    {
        if (!File.Exists(localPath))
        {
            return null;
        }

        var fileInfo = new FileInfo(localPath);
        return new FileEntry
        {
            RelativePath = fileInfo.Name.Replace('\\', '/'),
            IsDirectory = false,
            Fingerprint = new FileFingerprint
            {
                SizeBytes = fileInfo.Length,
                ModifiedTimeUtc = fileInfo.LastWriteTimeUtc
            }
        };
    }

    public Task EnsureDirectoryAsync(string directoryPath)
    {
        Directory.CreateDirectory(directoryPath);
        return Task.CompletedTask;
    }
}
