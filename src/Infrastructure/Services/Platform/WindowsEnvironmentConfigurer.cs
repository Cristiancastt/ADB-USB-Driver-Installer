using System.Runtime.InteropServices;
using AdbDriverInstaller.Core.Enums;
using AdbDriverInstaller.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System.Runtime.Versioning;

namespace AdbDriverInstaller.Infrastructure.Services.Platform;

[SupportedOSPlatform("windows")]
public sealed class WindowsEnvironmentConfigurer(ILogger<WindowsEnvironmentConfigurer> logger) : IEnvironmentConfigurer
{
    public Task<bool> AddToPathAsync(string directoryPath, InstallLevel level = InstallLevel.User, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            try
            {
                if (IsInPath(directoryPath))
                {
                    logger.LogInformation("Directory already in PATH: {DirectoryPath}", directoryPath);
                    return true;
                }

                if (level == InstallLevel.System)
                {
                    if (!TryWriteSystemPath(directoryPath))
                    {
                        // Fallback: if we can't write to HKLM, write to user PATH instead
                        logger.LogWarning("Cannot write to system PATH (not admin). Falling back to user PATH.");
                        if (!TryWriteUserPath(directoryPath))
                            return false;
                    }
                }
                else
                {
                    if (!TryWriteUserPath(directoryPath))
                        return false;
                }

                BroadcastEnvironmentChange();
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to add {DirectoryPath} to PATH", directoryPath);
                return false;
            }
        }, ct);
    }

    public bool IsInPath(string directoryPath)
    {
        var userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? string.Empty;
        var sysPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? string.Empty;
        var combined = $"{sysPath};{userPath}";
        return combined.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Any(p => string.Equals(p.Trim(), directoryPath, StringComparison.OrdinalIgnoreCase));
    }

    private bool TryWriteSystemPath(string directoryPath)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment", writable: true);
            if (key is null)
                return false;

            var currentPath = key.GetValue("Path", string.Empty) as string ?? string.Empty;
            if (ContainsPath(currentPath, directoryPath))
                return true;

            var newPath = string.IsNullOrWhiteSpace(currentPath)
                ? directoryPath
                : $"{currentPath};{directoryPath}";

            key.SetValue("Path", newPath, RegistryValueKind.ExpandString);
            logger.LogInformation("Added to system PATH: {DirectoryPath}", directoryPath);
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
        {
            logger.LogWarning("No permission to write system PATH");
            return false;
        }
    }

    private bool TryWriteUserPath(string directoryPath)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey("Environment", writable: true);
            if (key is null)
            {
                logger.LogError("Failed to open user Environment registry key");
                return false;
            }

            var currentPath = key.GetValue("Path", string.Empty) as string ?? string.Empty;
            if (ContainsPath(currentPath, directoryPath))
            {
                logger.LogInformation("Already in user PATH: {DirectoryPath}", directoryPath);
                return true;
            }

            var newPath = string.IsNullOrWhiteSpace(currentPath)
                ? directoryPath
                : $"{currentPath};{directoryPath}";

            key.SetValue("Path", newPath, RegistryValueKind.ExpandString);
            logger.LogInformation("Added to user PATH: {DirectoryPath}", directoryPath);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write user PATH");
            return false;
        }
    }

    private static bool ContainsPath(string pathValue, string directory)
    {
        return pathValue.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Any(p => string.Equals(p.Trim(), directory, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Broadcasts WM_SETTINGCHANGE so that Explorer and other apps pick up the new PATH.
    /// Fire-and-forget with a short timeout to avoid blocking.
    /// </summary>
    private static void BroadcastEnvironmentChange()
    {
        try
        {
            // Run on a separate thread to never block the caller
            _ = Task.Run(() =>
            {
                try
                {
                    _ = SendMessageTimeout(
                        HWND_BROADCAST, WM_SETTINGCHANGE,
                        nint.Zero, "Environment",
                        SMTO_ABORTIFHUNG, 2000, out _);
                }
                catch { }
            });
        }
        catch { }
    }

    private const int HWND_BROADCAST = 0xFFFF;
    private const int WM_SETTINGCHANGE = 0x001A;
    private const int SMTO_ABORTIFHUNG = 0x0002;

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern nint SendMessageTimeout(
        nint hWnd, int msg, nint wParam,
        string lParam, int fuFlags, int uTimeout, out nint lpdwResult);
}
