using System.IO;
using System.Reflection;

namespace AndroidPhotoSync.Gui;

public static class AdbLoader
{
    public static async Task<string> EnsureAdbAvailableAsync()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var targetDir = Path.Combine(appData, "AndroidPhotoSync", "bin");
        var adbPath = Path.Combine(targetDir, "adb.exe");

        if (File.Exists(adbPath))
        {
            // Verify checksum or simple existence? For now, simple existence.
            // But we might want to update it if the embedded version changes.
            // To keep it simple and fast, if it exists, we use it. 
            // Users can delete the bin folder to force refresh.
            return adbPath;
        }

        Directory.CreateDirectory(targetDir);

        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames();

        await ExtractResourceAsync(assembly, "AndroidPhotoSync.Gui.Resources.adb.exe", adbPath);
        await ExtractResourceAsync(assembly, "AndroidPhotoSync.Gui.Resources.AdbWinApi.dll", Path.Combine(targetDir, "AdbWinApi.dll"));
        await ExtractResourceAsync(assembly, "AndroidPhotoSync.Gui.Resources.AdbWinUsbApi.dll", Path.Combine(targetDir, "AdbWinUsbApi.dll"));

        return adbPath;
    }

    private static async Task ExtractResourceAsync(Assembly assembly, string resourceName, string targetPath)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            // Fallback: try to find resource ending with the name (ignoring namespace prefix changes)
            var foundName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(resourceName.Split('.').Last()));
            if (foundName is null) return;
            
            using var foundStream = assembly.GetManifestResourceStream(foundName);
            if (foundStream is null) return;
            
            using var fileStream = File.Create(targetPath);
            await foundStream.CopyToAsync(fileStream);
            return;
        }

        using var fs = File.Create(targetPath);
        await stream.CopyToAsync(fs);
    }
}
