using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using AndroidPhotoSync.Core.Abstractions;
using AndroidPhotoSync.Core.Models;

namespace AndroidPhotoSync.Infrastructure;

public record AdbDevice(string Serial, string Model, string State)
{
    public override string ToString() => $"{Model} ({Serial})";
}

public sealed class AdbRemoteFileProvider : IRemoteFileProvider
{
    private readonly string _adbPath;
    private readonly string? _deviceSerial;

    public AdbRemoteFileProvider(string adbPath, string? deviceSerial = null)
    {
        _adbPath = adbPath;
        _deviceSerial = deviceSerial;
    }

    public static async Task<List<AdbDevice>> GetConnectedDevicesAsync(string adbPath)
    {
        var result = await RunAdbCommandAsync(adbPath, null, new[] { "devices", "-l" }, CancellationToken.None);
        var devices = new List<AdbDevice>();
        
        if (result.ExitCode != 0) return devices;

        var lines = result.StandardOutput.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        // Skip first line "List of devices attached"
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            // Format: "serial state product:x model:y device:z transport_id:n"
            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;

            var serial = parts[0];
            var state = parts[1];
            var model = parts.FirstOrDefault(p => p.StartsWith("model:"))?.Substring(6) ?? "Unknown";

            devices.Add(new AdbDevice(serial, model, state));
        }

        return devices;
    }

    public async Task<IReadOnlyList<FileEntry>> ListFilesAsync(string remoteRoot, bool recursive, CancellationToken cancellationToken)
    {
        var batchResult = await TryListFilesWithStatBatch(remoteRoot, recursive, cancellationToken);
        if (batchResult is not null)
        {
            return batchResult;
        }

        return await ListFilesWithPerFileStat(remoteRoot, recursive, cancellationToken);
    }

    public async Task TransferFileToLocalAsync(string remotePath, string localPath, bool overwrite, CancellationToken cancellationToken)
    {
        if (File.Exists(localPath))
        {
            if (!overwrite)
            {
                throw new IOException($"目标文件已存在: {localPath}");
            }
            File.Delete(localPath);
        }

        var pullArgs = new[]
        {
            "pull",
            remotePath,
            localPath
        };

        var result = await RunAdbAsync(pullArgs, cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"ADB Pull Failed: {result.StandardError}");
        }
    }

    private async Task<FileFingerprint?> TryGetFingerprintAsync(string remotePath, CancellationToken cancellationToken)
    {
        var statArgs = new[]
        {
            "shell",
            $"stat -c %s|%Y {QuoteForShell(remotePath)}"
        };

        var result = await RunAdbAsync(statArgs, cancellationToken);
        if (result.ExitCode != 0)
        {
            return null;
        }

        var line = result.StandardOutput.Trim();
        var parts = line.Split('|');
        if (parts.Length != 2)
        {
            return null;
        }

        if (!long.TryParse(parts[0], out var size))
        {
            return null;
        }

        if (!long.TryParse(parts[1], out var epochSeconds))
        {
            return new FileFingerprint
            {
                SizeBytes = size
            };
        }

        var modified = DateTimeOffset.FromUnixTimeSeconds(epochSeconds);
        return new FileFingerprint
        {
            SizeBytes = size,
            ModifiedTimeUtc = modified
        };
    }

    private async Task<IReadOnlyList<FileEntry>?> TryListFilesWithStatBatch(string remoteRoot, bool recursive, CancellationToken cancellationToken)
    {
        // find /path -maxdepth 1 -type f -exec ...
        var depthOption = recursive ? "" : "-maxdepth 1";
        var command = $"find {QuoteForShell(remoteRoot)} {depthOption} -type f -exec stat -c '%n|%s|%Y' {{}} +";
        
        // Remove extra spaces if recursive is true
        command = command.Replace("  ", " ");

        var batchArgs = new[] { "shell", command };
        var result = await RunAdbAsync(batchArgs, cancellationToken);
        if (result.ExitCode != 0)
        {
            return null;
        }

        var files = new List<FileEntry>();
        var lines = result.StandardOutput
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            var lastSeparator = trimmed.LastIndexOf('|');
            if (lastSeparator <= 0)
            {
                continue;
            }

            var secondLastSeparator = trimmed.LastIndexOf('|', lastSeparator - 1);
            if (secondLastSeparator <= 0)
            {
                continue;
            }

            var remotePath = trimmed.Substring(0, secondLastSeparator);
            var sizeText = trimmed.Substring(secondLastSeparator + 1, lastSeparator - secondLastSeparator - 1);
            var timeText = trimmed.Substring(lastSeparator + 1);

            long? size = null;
            long? epochSeconds = null;
            if (long.TryParse(sizeText, out var parsedSize))
            {
                size = parsedSize;
            }
            if (long.TryParse(timeText, out var parsedTime))
            {
                epochSeconds = parsedTime;
            }

            var fingerprint = size.HasValue || epochSeconds.HasValue
                ? new FileFingerprint
                {
                    SizeBytes = size,
                    ModifiedTimeUtc = epochSeconds.HasValue ? DateTimeOffset.FromUnixTimeSeconds(epochSeconds.Value) : null
                }
                : null;

            files.Add(new FileEntry
            {
                RelativePath = GetRelativePath(remoteRoot, remotePath),
                IsDirectory = false,
                Fingerprint = fingerprint
            });
        }

        return files;
    }

    private async Task<IReadOnlyList<FileEntry>> ListFilesWithPerFileStat(string remoteRoot, bool recursive, CancellationToken cancellationToken)
    {
        var files = new List<FileEntry>();
        var depthOption = recursive ? "" : "-maxdepth 1";
        var findArgs = new[]
        {
            "shell",
            $"find {QuoteForShell(remoteRoot)} {depthOption} -type f"
        };

        var findResult = await RunAdbAsync(findArgs, cancellationToken);
        if (findResult.ExitCode != 0)
        {
            return files;
        }

        var lines = findResult.StandardOutput
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var remotePath = line.Trim();
            if (string.IsNullOrWhiteSpace(remotePath))
            {
                continue;
            }

            var relativePath = GetRelativePath(remoteRoot, remotePath);
            var fingerprint = await TryGetFingerprintAsync(remotePath, cancellationToken);
            
            files.Add(new FileEntry
            {
                RelativePath = relativePath,
                IsDirectory = false,
                Fingerprint = fingerprint
            });
        }

        return files;
    }

    private async Task<AdbResult> RunAdbAsync(string[] args, CancellationToken cancellationToken)
    {
        return await RunAdbCommandAsync(_adbPath, _deviceSerial, args, cancellationToken);
    }

    private static async Task<AdbResult> RunAdbCommandAsync(string adbPath, string? serial, string[] args, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = adbPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        if (!string.IsNullOrEmpty(serial))
        {
            startInfo.ArgumentList.Add("-s");
            startInfo.ArgumentList.Add(serial);
        }

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = startInfo };
        
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            return new AdbResult
            {
                ExitCode = process.ExitCode,
                StandardOutput = outputBuilder.ToString(),
                StandardError = errorBuilder.ToString()
            };
        }
        catch (Exception ex)
        {
            return new AdbResult
            {
                ExitCode = -1,
                StandardOutput = string.Empty,
                StandardError = ex.Message
            };
        }
    }

    private static string QuoteForShell(string path)
    {
        return $"'{path.Replace("'", "'\\''")}'";
    }

    private static string GetRelativePath(string root, string fullPath)
    {
        // Simple relative path logic
        // root: /sdcard/DCIM
        // full: /sdcard/DCIM/Camera/1.jpg
        // rel: Camera/1.jpg
        
        var normalizedRoot = root.TrimEnd('/');
        if (fullPath.StartsWith(normalizedRoot))
        {
            var rel = fullPath.Substring(normalizedRoot.Length);
            return rel.TrimStart('/');
        }
        return fullPath;
    }

    private class AdbResult
    {
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; } = string.Empty;
        public string StandardError { get; set; } = string.Empty;
    }
}
