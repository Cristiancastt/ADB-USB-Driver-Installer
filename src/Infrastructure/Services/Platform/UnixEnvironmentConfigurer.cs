using AdbDriverInstaller.Core.Enums;
using AdbDriverInstaller.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AdbDriverInstaller.Infrastructure.Services.Platform;

public sealed class UnixEnvironmentConfigurer(ILogger<UnixEnvironmentConfigurer> logger) : IEnvironmentConfigurer
{
    public async Task<bool> AddToPathAsync(string directoryPath, InstallLevel level = InstallLevel.User, CancellationToken ct = default)
    {
        try
        {
            if (IsInPath(directoryPath))
            {
                logger.LogInformation("Directory already in PATH: {DirectoryPath}", directoryPath);
                return true;
            }

            var exportLine = $"\nexport PATH=\"$PATH:{directoryPath}\"\n";

            if (level == InstallLevel.System)
            {
                // System-level: write to /etc/profile.d/
                var profileFile = "/etc/profile.d/adb-platform-tools.sh";
                try
                {
                    await File.WriteAllTextAsync(profileFile, exportLine, ct);
                    logger.LogInformation("Added system-wide PATH export to {ProfileFile}", profileFile);
                    return true;
                }
                catch (UnauthorizedAccessException)
                {
                    logger.LogError("Insufficient permissions to write to {ProfileFile}. Run with sudo.", profileFile);
                    return false;
                }
            }

            // User-level: append to shell config
            var shellConfigFile = GetShellConfigFile();
            if (shellConfigFile is null)
            {
                logger.LogError("Could not determine shell configuration file");
                return false;
            }

            if (File.Exists(shellConfigFile))
            {
                var content = await File.ReadAllTextAsync(shellConfigFile, ct);
                if (content.Contains(directoryPath, StringComparison.Ordinal))
                {
                    logger.LogInformation("PATH entry already exists in {ConfigFile}", shellConfigFile);
                    return true;
                }
            }

            await File.AppendAllTextAsync(shellConfigFile, exportLine, ct);
            logger.LogInformation("Added PATH export to {ConfigFile}. Open a new terminal for changes to take effect.", shellConfigFile);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add {DirectoryPath} to PATH", directoryPath);
            return false;
        }
    }

    public bool IsInPath(string directoryPath)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        return path.Split(':', StringSplitOptions.RemoveEmptyEntries)
            .Any(p => string.Equals(p.Trim(), directoryPath, StringComparison.Ordinal));
    }

    private static string? GetShellConfigFile()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(home)) return null;

        var shell = Environment.GetEnvironmentVariable("SHELL") ?? string.Empty;

        if (shell.EndsWith("zsh", StringComparison.OrdinalIgnoreCase))
            return Path.Combine(home, ".zshrc");

        if (shell.EndsWith("fish", StringComparison.OrdinalIgnoreCase))
            return Path.Combine(home, ".config", "fish", "config.fish");

        // Default to bash
        return Path.Combine(home, ".bashrc");
    }
}
