using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using AndroidPhotoSync.Core.Abstractions;
using AndroidPhotoSync.Core.Models;
using AndroidPhotoSync.Core.Services;
using AndroidPhotoSync.Infrastructure;
using Forms = System.Windows.Forms;

namespace AndroidPhotoSync.Gui;

public partial class MainWindow : Window
{
    private static readonly string[] MediaExtensions = { ".jpg", ".jpeg", ".png", ".heic", ".dng", ".webp", ".gif", ".bmp", ".tiff", ".mp4", ".mov", ".mkv", ".avi", ".3gp", ".flv", ".wmv" };

    public MainWindow()
    {
        InitializeComponent();
        RemotePathBox.Text = "/sdcard/DCIM";
        LocalPathBox.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "AndroidPhotoSync");
        
        Loaded += async (s, e) => await RefreshDevicesAsync();
    }

    private void OnBrowseLocalClicked(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "选择备份目录",
            UseDescriptionForTitle = true,
            SelectedPath = string.IsNullOrWhiteSpace(LocalPathBox.Text) ? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) : LocalPathBox.Text
        };
        var result = dialog.ShowDialog();
        if (result == Forms.DialogResult.OK)
        {
            LocalPathBox.Text = dialog.SelectedPath;
        }
    }

    private async void OnRefreshDevicesClicked(object sender, RoutedEventArgs e)
    {
        await RefreshDevicesAsync();
    }

    private async Task RefreshDevicesAsync()
    {
        var adbPath = await AdbLoader.EnsureAdbAvailableAsync();
        if (string.IsNullOrWhiteSpace(adbPath))
        {
            StatusText.Text = "未找到内置 ADB 工具";
            return;
        }

        try
        {
            StatusText.Text = "正在扫描设备...";
            var devices = await AdbRemoteFileProvider.GetConnectedDevicesAsync(adbPath);
            DeviceComboBox.ItemsSource = devices;
            
            if (devices.Count > 0)
            {
                DeviceComboBox.SelectedIndex = 0;
                StatusText.Text = $"扫描到 {devices.Count} 台设备";
            }
            else
            {
                StatusText.Text = "未检测到设备";
                ResultText.Text = "请检查 USB 连接和开发者选项设置。";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = "扫描设备失败";
            ResultText.Text = $"ADB Error:\n{ex.Message}";
        }
    }

    private async void OnStartSyncClicked(object sender, RoutedEventArgs e)
    {
        var remoteRoot = RemotePathBox.Text.Trim();
        var localRoot = LocalPathBox.Text.Trim();
        var adbPath = await AdbLoader.EnsureAdbAvailableAsync();
        var selectedDevice = DeviceComboBox.SelectedItem as AdbDevice;

        if (string.IsNullOrWhiteSpace(remoteRoot) || string.IsNullOrWhiteSpace(localRoot))
        {
            StatusText.Text = "请填写手机目录与备份目录";
            return;
        }

        if (string.IsNullOrWhiteSpace(adbPath))
        {
            StatusText.Text = "未找到内置 ADB 工具";
            return;
        }

        if (selectedDevice is null)
        {
            StatusText.Text = "请选择一台设备";
            return;
        }

        StartButton.IsEnabled = false;
        StatusText.Text = "同步进行中...";
        ResultText.Text = "初始化同步引擎...\n";

        // Create meta directory in LocalApplicationData
        var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AndroidPhotoSync");
        var checkpointDir = Path.Combine(appDataDir, "checkpoints");
        var logDir = Path.Combine(appDataDir, "logs");
        
        // Append the remote directory name to the local root
        // e.g. /sdcard/DCIM -> D:\Backup\DCIM
        // e.g. /sdcard/DCIM/Camera -> D:\Backup\Camera
        var remoteDirName = Path.GetFileName(remoteRoot.TrimEnd('/', '\\'));
        if (!string.IsNullOrWhiteSpace(remoteDirName))
        {
            localRoot = Path.Combine(localRoot, remoteDirName);
        }

        Directory.CreateDirectory(localRoot);
        Directory.CreateDirectory(checkpointDir);
        Directory.CreateDirectory(logDir);

        var checkpointKey = CreateCheckpointKey(remoteRoot, localRoot);
        var logPath = Path.Combine(logDir, $"sync_{DateTime.Now:yyyyMMdd_HHmmss}.log");

        IRemoteFileProvider remoteProvider = new AdbRemoteFileProvider(adbPath, selectedDevice.Serial);
        ILocalFileProvider localProvider = new FileSystemLocalFileProvider();
        ICheckpointStore checkpointStore = new FileCheckpointStore(checkpointDir);
        ILogWriter logWriter = new FileLogWriter(logPath);

        var engine = new SyncEngine(remoteProvider, localProvider, checkpointStore, logWriter);
        
        var isRecursive = RecursiveBox.IsChecked == true;
        var backupAll = BackupAllBox.IsChecked == true;
        
        var options = new SyncOptions
        {
            RemoteRoot = remoteRoot,
            LocalRoot = localRoot,
            CheckpointKey = checkpointKey,
            OverwriteExisting = false,
            Recursive = isRecursive,
            AllowedExtensions = backupAll ? null : MediaExtensions
        };

        try
        {
            var result = await engine.ExecuteAsync(options, CancellationToken.None);
            StatusText.Text = "同步完成";
            ResultText.Text = $"同步成功!\n\n" +
                              $"总计文件: {result.TotalFiles}\n" +
                              $"成功复制: {result.CopiedFiles}\n" +
                              $"跳过文件: {result.SkippedFiles}\n" +
                              $"冲突重名: {result.ConflictCopies}\n" +
                              $"失败文件: {result.ErrorFiles}\n\n" +
                              $"日志位置: {logPath}";
        }
        catch (Exception ex)
        {
            StatusText.Text = "同步过程中发生错误";
            ResultText.Text = $"错误详情:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
        }
        finally
        {
            StartButton.IsEnabled = true;
        }
    }

    private static string CreateCheckpointKey(string remoteRoot, string localRoot)
    {
        using var sha256 = SHA256.Create();
        var input = Encoding.UTF8.GetBytes($"{remoteRoot}|{localRoot}");
        var hash = sha256.ComputeHash(input);
        return Convert.ToHexString(hash);
    }
}
