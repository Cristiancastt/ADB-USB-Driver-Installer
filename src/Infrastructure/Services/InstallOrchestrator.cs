using AdbDriverInstaller.Core.Enums;
using AdbDriverInstaller.Core.Interfaces;
using AdbDriverInstaller.Core.Models;
using AdbDriverInstaller.Infrastructure.Services.Platform;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AdbDriverInstaller.Infrastructure.Services;

public sealed class InstallOrchestrator(
    IPlatformDetector platformDetector,
    IDownloadService downloadService,
    IFileExtractor fileExtractor,
    IEnvironmentConfigurer environmentConfigurer,
    IDriverInstaller driverInstaller,
    IAdbVerifier adbVerifier,
    UnixPermissionHelper unixPermissionHelper,
    ILogger<InstallOrchestrator> logger) : IInstallOrchestrator
{
    public Action<string>? OnStatusUpdate { get; set; }
    public IProgress<double>? DownloadProgress { get; set; }
    public IProgress<double>? ExtractProgress { get; set; }

    public async Task<InstallResult> InstallAsync(InstallOptions options, CancellationToken ct = default)
    {
        string? installPath = null;
        var rollbackPaths = new List<string>();

        try
        {
            // 1. Detect platform
            var platform = platformDetector.Detect();
            installPath = options.InstallPath ?? platformDetector.GetDefaultInstallPath();
            var parentDir = Path.GetDirectoryName(installPath)!;
            var tempDir = Path.Combine(Path.GetTempPath(), "adb-driver-installer");

            logger.LogInformation("Platform: {Platform}, Install path: {Path}", platform.Platform, installPath);
            OnStatusUpdate?.Invoke($"Detected platform: {platform.Platform}");

            // 2. Verify disk space (need ~200MB for download + extraction)
            const long requiredBytes = 200 * 1024 * 1024;
            if (!HasEnoughDiskSpace(parentDir, requiredBytes))
            {
                return InstallResult.Fail($"Not enough disk space. Need at least 200 MB free on {Path.GetPathRoot(parentDir)}.");
            }

            // 3. Kill running adb/fastboot to release file locks
            KillProcesses("adb", "fastboot");

            // 4. Download platform tools
            OnStatusUpdate?.Invoke("Downloading Android Platform Tools...");
            string platformToolsZip;
            try
            {
                platformToolsZip = await downloadService.DownloadFileAsync(
                    platform.PlatformToolsUrl, tempDir, DownloadProgress, ct);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Failed to download platform tools from {Url}", platform.PlatformToolsUrl);
                return InstallResult.Fail($"Failed to download Platform Tools: {ex.Message}");
            }

            // 5. Extract platform tools
            OnStatusUpdate?.Invoke("Extracting Platform Tools...");
            string extractedPath;
            try
            {
                extractedPath = await fileExtractor.ExtractZipAsync(platformToolsZip, parentDir, ExtractProgress, ct);
            }
            catch (InvalidDataException ex)
            {
                logger.LogError(ex, "Downloaded file is not a valid zip archive");
                return InstallResult.Fail($"Corrupted download — the file is not a valid zip: {ex.Message}");
            }

            // Rename if needed to match desired install path
            if (!string.Equals(extractedPath, installPath, StringComparison.OrdinalIgnoreCase)
                && extractedPath != installPath)
            {
                if (Directory.Exists(installPath))
                    Directory.Delete(installPath, recursive: true);

                Directory.Move(extractedPath, installPath);
            }
            rollbackPaths.Add(installPath);

            // 6. Set executable permissions on Unix
            if (platform.Platform is PlatformType.Linux or PlatformType.MacOS)
            {
                OnStatusUpdate?.Invoke("Setting executable permissions...");
                await unixPermissionHelper.SetExecutablePermissionsAsync(installPath, ct);
            }

            // 6. Install USB drivers (Windows only)
            var usbDriversInstalled = false;
            if (options.InstallUsbDrivers && driverInstaller.IsSupported && platform.UsbDriverUrl is not null)
            {
                try
                {
                    OnStatusUpdate?.Invoke("Downloading USB drivers...");
                    var usbDriverZip = await downloadService.DownloadFileAsync(
                        platform.UsbDriverUrl, tempDir, DownloadProgress, ct);

                    OnStatusUpdate?.Invoke("Extracting USB drivers...");
                    var usbDriverPath = await fileExtractor.ExtractZipAsync(usbDriverZip, tempDir, ExtractProgress, ct);

                    OnStatusUpdate?.Invoke("Installing USB drivers (may require admin privileges)...");
                    usbDriversInstalled = await driverInstaller.InstallUsbDriversAsync(usbDriverPath, ct);

                    if (!usbDriversInstalled)
                        logger.LogWarning("USB driver installation returned false — drivers may not have been installed");
                }
                catch (HttpRequestException ex)
                {
                    logger.LogError(ex, "Failed to download USB drivers from {Url}", platform.UsbDriverUrl);
                }
                catch (InvalidDataException ex)
                {
                    logger.LogError(ex, "USB driver zip is corrupted");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "USB driver installation failed");
                }
            }

            // 7. Add to PATH
            var pathConfigured = false;
            if (options.AddToPath)
            {
                OnStatusUpdate?.Invoke("Configuring PATH environment variable...");
                try
                {
                    pathConfigured = await environmentConfigurer.AddToPathAsync(installPath, options.Level, ct);
                    if (!pathConfigured)
                        logger.LogWarning("PATH configuration returned false — may require elevated permissions");
                }
                catch (UnauthorizedAccessException ex)
                {
                    logger.LogError(ex, "Insufficient permissions to modify PATH");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Failed to configure PATH");
                }
            }

            // 8. Configure udev rules (Linux only)
            if (platform.Platform is PlatformType.Linux)
            {
                try
                {
                    OnStatusUpdate?.Invoke("Configuring udev rules for USB device access...");
                    await ConfigureUdevRulesAsync(ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Failed to configure udev rules — device access may require root");
                }
            }

            // 9. Verify installation
            string? adbVersion = null;
            string? fastbootVersion = null;
            if (options.VerifyAfterInstall)
            {
                OnStatusUpdate?.Invoke("Verifying installation...");
                try
                {
                    var verification = await adbVerifier.VerifyAsync(installPath, ct);
                    adbVersion = verification.AdbVersion;
                    fastbootVersion = verification.FastbootVersion;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Verification step failed but installation may still be valid");
                }
            }

            // 10. Cleanup temp files
            CleanupTempDirectory(tempDir);

            return InstallResult.Ok(installPath, adbVersion, fastbootVersion, pathConfigured, usbDriversInstalled);
        }
        catch (OperationCanceledException)
        {
            Rollback(rollbackPaths);
            return InstallResult.Fail("Installation was cancelled.");
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogError(ex, "Permission denied during installation");
            Rollback(rollbackPaths);
            return InstallResult.Fail($"Permission denied: {ex.Message}");
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "File system error during installation");
            Rollback(rollbackPaths);
            return InstallResult.Fail($"File system error: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Installation failed");
            Rollback(rollbackPaths);
            return InstallResult.Fail(ex.Message);
        }
    }

    private void KillProcesses(params string[] processNames)
    {
        foreach (var name in processNames)
        {
            try
            {
                foreach (var proc in Process.GetProcessesByName(name))
                {
                    logger.LogInformation("Killing running {Process} (PID {Pid}) to release file locks", name, proc.Id);
                    proc.Kill();
                    proc.WaitForExit(5000);
                    proc.Dispose();
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to kill {Process}", name);
            }
        }
    }

    private void CleanupTempDirectory(string tempDir)
    {
        try
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to clean up temp directory: {Path}", tempDir);
        }
    }

    private bool HasEnoughDiskSpace(string path, long requiredBytes)
    {
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(path));
            if (root is null) return true;

            var drive = new DriveInfo(root);
            if (drive.IsReady && drive.AvailableFreeSpace < requiredBytes)
            {
                logger.LogError("Insufficient disk space on {Drive}: {Available} MB available, {Required} MB required",
                    root, drive.AvailableFreeSpace / 1024 / 1024, requiredBytes / 1024 / 1024);
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not check disk space — proceeding anyway");
        }

        return true;
    }

    private void Rollback(List<string> paths)
    {
        foreach (var path in paths)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    logger.LogInformation("Rolling back: deleting {Path}", path);
                    Directory.Delete(path, recursive: true);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Rollback failed for {Path}", path);
            }
        }
    }

    private async Task ConfigureUdevRulesAsync(CancellationToken ct)
    {
        const string rulesPath = "/etc/udev/rules.d/51-android.rules";
        const string rulesContent = """
            # Android ADB/Fastboot device access
            SUBSYSTEM=="usb", ATTR{idVendor}=="18d1", MODE="0666", GROUP="plugdev"
            SUBSYSTEM=="usb", ATTR{idVendor}=="04e8", MODE="0666", GROUP="plugdev"
            SUBSYSTEM=="usb", ATTR{idVendor}=="0bb4", MODE="0666", GROUP="plugdev"
            SUBSYSTEM=="usb", ATTR{idVendor}=="22b8", MODE="0666", GROUP="plugdev"
            SUBSYSTEM=="usb", ATTR{idVendor}=="054c", MODE="0666", GROUP="plugdev"
            SUBSYSTEM=="usb", ATTR{idVendor}=="2717", MODE="0666", GROUP="plugdev"
            SUBSYSTEM=="usb", ATTR{idVendor}=="1949", MODE="0666", GROUP="plugdev"
            SUBSYSTEM=="usb", ATTR{idVendor}=="2a70", MODE="0666", GROUP="plugdev"
            SUBSYSTEM=="usb", ATTR{idVendor}=="05c6", MODE="0666", GROUP="plugdev"
            SUBSYSTEM=="usb", ATTR{idVendor}=="1bbb", MODE="0666", GROUP="plugdev"
            SUBSYSTEM=="usb", ATTR{idVendor}=="2916", MODE="0666", GROUP="plugdev"
            """;

        if (File.Exists(rulesPath))
        {
            logger.LogInformation("udev rules already exist at {Path}", rulesPath);
            return;
        }

        try
        {
            await File.WriteAllTextAsync(rulesPath, rulesContent, ct);
            logger.LogInformation("Wrote udev rules to {Path}", rulesPath);

            // Reload udev rules
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "udevadm",
                    Arguments = "control --reload-rules",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            await process.WaitForExitAsync(ct);
        }
        catch (UnauthorizedAccessException)
        {
            logger.LogWarning("No permission to write udev rules — run with sudo for USB device access without root");
        }
    }
}
