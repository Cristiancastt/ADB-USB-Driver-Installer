using System.Runtime.Versioning;
using AdbDriverInstaller.Core.Enums;
using AdbDriverInstaller.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace AdbDriverInstaller.Infrastructure.Services.Platform;

[SupportedOSPlatform("windows")]
public sealed class WindowsEnvironmentConfigurer(ILogger<WindowsEnvironmentConfigurer> logger) : IEnvironmentConfigurer
{
    public Task<bool> AddToPathAsync(string directoryPath, InstallLevel level = InstallLevel.User, CancellationToken ct = default)
    {
        try
        {
            if (IsInPath(directoryPath))
            {
                logger.LogInformation("Directory already in PATH: {DirectoryPath}", directoryPath);
                return Task.FromResult(true);
            }

            if (level == InstallLevel.System)
            {
                // System-level PATH via HKLM
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\Session Manager\Environment", writable: true);
                if (key is null)
                {
                    logger.LogError("Failed to open system Environment registry key. Run as administrator.");
                    return Task.FromResult(false);
                }

                var currentPath = key.GetValue("Path", string.Empty) as string ?? string.Empty;
                var newPath = string.IsNullOrWhiteSpace(currentPath)
                    ? directoryPath
                    : $"{currentPath};{directoryPath}";

                key.SetValue("Path", newPath, RegistryValueKind.ExpandString);
                logger.LogInformation("Added to system PATH: {DirectoryPath}", directoryPath);
            }
            else
            {
                // User-level PATH via HKCU
                using var key = Registry.CurrentUser.OpenSubKey("Environment", writable: true);
                if (key is null)
                {
                    logger.LogError("Failed to open user Environment registry key");
                    return Task.FromResult(false);
                }

                var currentPath = key.GetValue("Path", string.Empty) as string ?? string.Empty;
                var newPath = string.IsNullOrWhiteSpace(currentPath)
                    ? directoryPath
                    : $"{currentPath};{directoryPath}";

                key.SetValue("Path", newPath, RegistryValueKind.ExpandString);
                logger.LogInformation("Added to user PATH: {DirectoryPath}", directoryPath);
            }

            return Task.FromResult(true);
        }
        catch (UnauthorizedAccessException)
        {
            logger.LogError("Insufficient permissions to modify {Level} PATH. Run as administrator.", level);
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add {DirectoryPath} to PATH", directoryPath);
            return Task.FromResult(false);
        }
    }

    public bool IsInPath(string directoryPath)
    {
        var userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? string.Empty;
        var sysPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? string.Empty;
        var combined = $"{sysPath};{userPath}";
        return combined.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Any(p => string.Equals(p.Trim(), directoryPath, StringComparison.OrdinalIgnoreCase));
    }
}
