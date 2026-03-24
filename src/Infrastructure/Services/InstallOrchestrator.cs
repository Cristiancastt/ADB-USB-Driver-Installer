using System.Net.Http;
using AdbDriverInstaller.Core.Enums;
using AdbDriverInstaller.Core.Interfaces;
using AdbDriverInstaller.Core.Models;
using AdbDriverInstaller.Infrastructure.Services.Platform;
using Microsoft.Extensions.Logging;

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

        try
        {
            // 1. Detect platform
            var platform = platformDetector.Detect();
            installPath = options.InstallPath ?? platformDetector.GetDefaultInstallPath();
            var parentDir = Path.GetDirectoryName(installPath)!;
            var tempDir = Path.Combine(Path.GetTempPath(), "adb-driver-installer");

            logger.LogInformation("Platform: {Platform}, Install path: {Path}", platform.Platform, installPath);
            OnStatusUpdate?.Invoke($"Detected platform: {platform.Platform}");

            // 2. Download platform tools
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

            // 3. Extract platform tools
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

            // 4. Set executable permissions on Unix
            if (platform.Platform is PlatformType.Linux or PlatformType.MacOS)
            {
                OnStatusUpdate?.Invoke("Setting executable permissions...");
                await unixPermissionHelper.SetExecutablePermissionsAsync(installPath, ct);
            }

            // 5. Install USB drivers (Windows only)
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
                    // Non-fatal: continue without USB drivers
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

            // 6. Add to PATH
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
                    // Non-fatal: platform tools are installed, PATH just wasn't updated
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Failed to configure PATH");
                }
            }

            // 7. Verify installation
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

            // 8. Cleanup temp files
            CleanupTempDirectory(tempDir);

            return InstallResult.Ok(installPath, adbVersion, fastbootVersion, pathConfigured, usbDriversInstalled);
        }
        catch (OperationCanceledException)
        {
            return InstallResult.Fail("Installation was cancelled.");
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogError(ex, "Permission denied during installation");
            return InstallResult.Fail($"Permission denied: {ex.Message}");
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "File system error during installation");
            return InstallResult.Fail($"File system error: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Installation failed");
            return InstallResult.Fail(ex.Message);
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
}
