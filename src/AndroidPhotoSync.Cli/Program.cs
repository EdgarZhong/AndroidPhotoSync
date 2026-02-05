﻿﻿﻿﻿﻿﻿﻿using System.Security.Cryptography;
using System.Text;
using AndroidPhotoSync.Core.Abstractions;
using AndroidPhotoSync.Core.Models;
using AndroidPhotoSync.Core.Services;
using AndroidPhotoSync.Infrastructure;

var arguments = ParseArguments(args);
if (!arguments.TryGetValue("remote", out var remoteRoot) || string.IsNullOrWhiteSpace(remoteRoot) ||
    !arguments.TryGetValue("local", out var localRoot) || string.IsNullOrWhiteSpace(localRoot))
{
    Console.WriteLine("用法: --remote <手机路径> --local <备份目录> [--adb <adb路径>] [--checkpoint <key>] [--checkpointDir <目录>] [--log <日志文件>]");
    return;
}

var adbPath = arguments.TryGetValue("adb", out var adbValue) && !string.IsNullOrWhiteSpace(adbValue)
    ? adbValue
    : FindAdbPath();

if (string.IsNullOrWhiteSpace(adbPath) || !File.Exists(adbPath))
{
    Console.WriteLine("未找到 adb.exe，请使用 --adb 指定完整路径");
    return;
}

Directory.CreateDirectory(localRoot);

var checkpointKey = arguments.TryGetValue("checkpoint", out var checkpointValue) && !string.IsNullOrWhiteSpace(checkpointValue)
    ? checkpointValue
    : CreateCheckpointKey(remoteRoot, localRoot);

var checkpointDir = arguments.TryGetValue("checkpointDir", out var checkpointDirValue) && !string.IsNullOrWhiteSpace(checkpointDirValue)
    ? checkpointDirValue
    : Path.Combine(localRoot, ".aps", "checkpoints");

var logPath = arguments.TryGetValue("log", out var logValue) && !string.IsNullOrWhiteSpace(logValue)
    ? logValue
    : Path.Combine(localRoot, ".aps", "logs", "sync.log");

IRemoteFileProvider remoteProvider = new AdbRemoteFileProvider(adbPath);
ILocalFileProvider localProvider = new FileSystemLocalFileProvider();
ICheckpointStore checkpointStore = new FileCheckpointStore(checkpointDir);
ILogWriter logWriter = new FileLogWriter(logPath);

var engine = new SyncEngine(remoteProvider, localProvider, checkpointStore, logWriter);
var options = new SyncOptions
{
    RemoteRoot = remoteRoot,
    LocalRoot = localRoot,
    CheckpointKey = checkpointKey,
    OverwriteExisting = false
};

Console.WriteLine("同步开始...");
var result = await engine.ExecuteAsync(options, CancellationToken.None);
Console.WriteLine($"同步结束: 总计={result.TotalFiles} 复制={result.CopiedFiles} 跳过={result.SkippedFiles} 冲突={result.ConflictCopies} 失败={result.ErrorFiles}");

static Dictionary<string, string> ParseArguments(string[] args)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var index = 0; index < args.Length; index++)
    {
        var arg = args[index];
        if (!arg.StartsWith("--", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        var key = arg.TrimStart('-');
        var value = string.Empty;
        if (index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.OrdinalIgnoreCase))
        {
            value = args[index + 1];
            index++;
        }
        result[key] = value;
    }
    return result;
}

static string CreateCheckpointKey(string remoteRoot, string localRoot)
{
    using var sha256 = SHA256.Create();
    var input = Encoding.UTF8.GetBytes($"{remoteRoot}|{localRoot}");
    var hash = sha256.ComputeHash(input);
    return Convert.ToHexString(hash);
}

static string FindAdbPath()
{
    var current = AppContext.BaseDirectory;
    for (var i = 0; i < 6; i++)
    {
        var candidate = Path.Combine(current, "platform-tools", "adb.exe");
        if (File.Exists(candidate))
        {
            return candidate;
        }
        var parent = Directory.GetParent(current);
        if (parent is null)
        {
            break;
        }
        current = parent.FullName;
    }
    return string.Empty;
}
