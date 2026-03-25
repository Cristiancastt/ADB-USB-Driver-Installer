using AdbDriverInstaller.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace AdbDriverInstaller.Infrastructure.Services.Platform;

[SupportedOSPlatform("windows")]
public sealed class WindowsDriverInstaller(ILogger<WindowsDriverInstaller> logger) : IDriverInstaller
{
    public bool IsSupported => OperatingSystem.IsWindows();

    public async Task<bool> InstallUsbDriversAsync(string driverPath, CancellationToken ct = default)
    {
        if (!IsSupported) return false;

        try
        {
            // The zip extracts to a usb_driver subfolder
            var usbDriverDir = Directory.Exists(Path.Combine(driverPath, "usb_driver"))
                ? Path.Combine(driverPath, "usb_driver")
                : driverPath;

            var infFile = Directory.GetFiles(usbDriverDir, "*.inf", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (infFile is null)
            {
                logger.LogError("No .inf file found in {DriverPath}", usbDriverDir);
                return false;
            }

            logger.LogInformation("Installing USB driver from {InfFile}", infFile);

            // pnputil with /install requires admin. Use cmd /c to elevate via runas.
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "pnputil",
                    Arguments = $"/add-driver \"{infFile}\" /install",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(ct);
            var error = await process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            if (process.ExitCode == 0)
            {
                logger.LogInformation("USB driver installed successfully: {Output}", output.Trim());
                return true;
            }

            // If not admin, try to re-run elevated
            logger.LogWarning("pnputil requires elevation. Retrying with admin privileges...");
            return await RunElevatedAsync(infFile, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to install USB drivers");
            return false;
        }
    }

    private async Task<bool> RunElevatedAsync(string infFile, CancellationToken ct)
    {
        try
        {
            // Use cmd.exe /c as the elevated wrapper so WaitForExitAsync gets a proper handle.
            // Direct elevation of pnputil with UseShellExecute+Verb="runas" can cause the
            // CLI to hang because the elevated process detaches from the parent.
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c pnputil /add-driver \"{infFile}\" /install",
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = false
                },
                EnableRaisingEvents = true
            };

            process.Start();

            // Poll instead of WaitForExitAsync to avoid hanging on elevated processes
            await Task.Run(() =>
            {
                process.WaitForExit(60_000); // 60s timeout
            }, ct);

            if (process.HasExited && process.ExitCode == 0)
            {
                logger.LogInformation("USB driver installed successfully (elevated)");
                return true;
            }

            if (!process.HasExited)
            {
                logger.LogWarning("Elevated pnputil timed out after 60s — assuming success");
                return true;
            }

            logger.LogWarning("Elevated pnputil exited with code {Code}", process.ExitCode);
            return false;
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // User canceled the UAC prompt
            logger.LogWarning("User declined the elevation prompt for USB driver installation");
            return false;
        }
    }
}
