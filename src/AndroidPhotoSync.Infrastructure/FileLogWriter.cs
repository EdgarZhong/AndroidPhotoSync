using System.Text.Json;
using AndroidPhotoSync.Core.Abstractions;
using AndroidPhotoSync.Core.Models;

namespace AndroidPhotoSync.Infrastructure;

public sealed class FileLogWriter : ILogWriter
{
    private readonly string _logFilePath;
    private readonly JsonSerializerOptions _serializerOptions;

    public FileLogWriter(string logFilePath)
    {
        _logFilePath = logFilePath;
        _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = false
        };
    }

    public async Task WriteAsync(SyncLogEntry entry, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(entry, _serializerOptions);
        await File.AppendAllTextAsync(_logFilePath, json + Environment.NewLine, cancellationToken);
    }
}
