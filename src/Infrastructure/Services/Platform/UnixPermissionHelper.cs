using System.Diagnostics;
using AdbDriverInstaller.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AdbDriverInstaller.Infrastructure.Services.Platform;

/// <summary>
/// Sets executable permissions on ADB/fastboot binaries for Unix-like systems.
/// </summary>
public sealed class UnixPermissionHelper(ILogger<UnixPermissionHelper> logger)
{
    public async Task SetExecutablePermissionsAsync(string directory, CancellationToken ct = default)
    {
        var binaries = new[] { "adb", "fastboot" };

        foreach (var binary in binaries)
        {
            var path = Path.Combine(directory, binary);
            if (!File.Exists(path)) continue;

            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = $"+x \"{path}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync(ct);
                logger.LogInformation("Set executable permission on {Path}", path);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to set executable permission on {Path}", path);
            }
        }
    }
}
