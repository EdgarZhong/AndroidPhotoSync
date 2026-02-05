# AndroidPhotoSync

> A minimalist, high-speed incremental photo backup tool for Android, built on ADB.
>
> 极简、高速的安卓照片增量备份工具，基于 ADB 协议。

[中文文档 (Chinese Documentation)](#中文文档)

## Introduction
AndroidPhotoSync is a minimalist photo synchronization tool designed for photographers and Android users. It follows the "Do One Thing Well" philosophy, focusing on backing up photos and videos from Android devices to Windows PCs quickly and stably via incremental sync.

**Core Philosophy:**
- **Minimalism**: No complex configuration, no background services, works out of the box.
- **Portable & Clean**: Single-file EXE, no registry modifications, no local runtime dependencies.
- **Incremental Sync**: Intelligently identifies backed-up files, supports resume from breakpoint.

## Features
- **USB Direct Sync**: High-speed transfer via USB connection using built-in ADB protocol.
- **Auto Device Recognition**: Automatically scans and lists connected Android devices.
- **Incremental Backup**: Skips existing unmodified files based on file size and modification time.
- **Resume Capability**: Resumes sync from the breakpoint if interrupted.
- **Clean Directory**: Metadata and logs are stored in AppData, keeping your backup folder clean.
- **Smart Path Mapping**: Automatically maps source folder names to backup subdirectories (e.g., `.../Camera` -> `D:\Backup\Camera`).
- **Flexible Options**:
    - Recursive sync support.
    - Option to backup only media files (default) or all files.

## Quick Start

1.  **Preparation**
    - Ensure **USB Debugging** is enabled on your phone (in "Developer Options").
    - Connect your phone to PC via USB cable.
    - Click **Allow** on the "Allow USB debugging" prompt on your phone upon first connection.

2.  **Run**
    - Double-click `AndroidPhotoSync.Gui.exe`.

3.  **Configure**
    - **Remote Path**: Enter the source folder on phone (e.g., `/sdcard/DCIM/Camera`).
    - **Local Path**: Select the destination folder on PC.
    - **Select Device**: Choose your device from the dropdown (click "Refresh Devices" if empty).

4.  **Start Backup**
    - Click **"Start Sync"**.
    - The console at the bottom will show real-time progress.

---

<a id="中文文档"></a>
# AndroidPhotoSync (中文文档)

## 简介
AndroidPhotoSync 是一款专为摄影师和安卓用户设计的极简照片同步工具。它遵循“不做多余之事”的设计哲学，专注于将安卓设备中的照片和视频快速、稳定地增量备份到 Windows 电脑。

**核心理念：**
- **极简主义**：无复杂配置，无后台服务，即开即用。
- **便携纯净**：单文件 EXE 运行，不修改注册表，不依赖本地运行时。
- **增量同步**：智能识别已备份文件，支持断点续传。

## 功能特性
- **USB 直连同步**：通过 USB 连接安卓设备，利用内置 ADB 协议高速传输。
- **自动设备识别**：自动扫描并列出已连接的安卓设备。
- **增量备份**：基于文件大小和修改时间比对，跳过已存在且未修改的文件。
- **断点续传**：意外中断后，再次运行可从断点处继续同步。
- **纯净目录**：元数据和日志存储在 AppData 目录，不污染用户的照片备份文件夹。
- **智能路径映射**：自动将手机源文件夹名称作为备份子目录（如 `.../Camera` -> `D:\Backup\Camera`）。
- **灵活选项**：
    - 支持递归同步子文件夹。
    - 支持仅备份媒体文件（默认）或备份所有文件。

## Quick Start (快速开始)

1.  **准备工作**
    - 确保手机已开启 **USB 调试模式**（在“开发者选项”中开启）。
    - 使用数据线将手机连接至电脑。
    - 首次连接时，请在手机弹出的“允许 USB 调试”对话框中点击**允许**。

2.  **运行软件**
    - 双击运行 `AndroidPhotoSync.Gui.exe`。

3.  **配置同步**
    - **手机目录**：输入要备份的手机文件夹路径（例如 `/sdcard/DCIM/Camera`）。
    - **备份目录**：选择电脑上的目标存储文件夹。
    - **选择设备**：在下拉列表中选择你的手机（如果未显示，点击“刷新设备”）。

4.  **开始备份**
    - 点击 **“开始同步”** 按钮。
    - 底部控制台将实时显示同步进度和结果。
    - 报错信息可直接复制以便排查。

## 常见问题
- **找不到设备？**
    - 请检查 USB 线是否连接良好。
    - 确认手机已开启 USB 调试模式。
    - 尝试重新拔插 USB 线并点击“刷新设备”。

- **备份速度慢？**
    - 速度主要取决于 USB 接口协议（USB 2.0 vs 3.0）和大量小文件的处理开销。
    - 建议使用原装或高质量 USB 3.0 数据线连接至电脑 USB 3.0 接口。

## 构建指南
本项目基于 .NET 8 WPF 开发。

**依赖环境：**
- .NET 8 SDK
- Visual Studio 2022 或 VS Code

**编译命令：**
```powershell
# 还原并构建解决方案
dotnet build AndroidPhotoSync.sln

# 发布单文件 EXE
dotnet publish src/AndroidPhotoSync.Gui/AndroidPhotoSync.Gui.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish_output
```
