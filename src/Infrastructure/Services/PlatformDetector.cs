using AdbDriverInstaller.Core.Configuration;
using AdbDriverInstaller.Core.Enums;
using AdbDriverInstaller.Core.Interfaces;
using AdbDriverInstaller.Core.Models;
using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;

namespace AdbDriverInstaller.Infrastructure.Services;

public sealed class PlatformDetector(IOptions<AdbInstallerConfig> config) : IPlatformDetector
{
    private readonly AdbInstallerConfig _config = config.Value;

    public PlatformInfo Detect()
    {
        var baseUrl = _config.BaseUrl.TrimEnd('/');

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new PlatformInfo(
                PlatformType.Windows,
                $"{baseUrl}/{_config.PlatformTools.Windows}",
                _config.UsbDriver.Windows is not null ? $"{baseUrl}/{_config.UsbDriver.Windows}" : null);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new PlatformInfo(
                PlatformType.MacOS,
                $"{baseUrl}/{_config.PlatformTools.MacOS}");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return new PlatformInfo(
                PlatformType.Linux,
                $"{baseUrl}/{_config.PlatformTools.Linux}");

        throw new PlatformNotSupportedException("This operating system is not supported.");
    }

    public string GetDefaultInstallPath(InstallLevel level = InstallLevel.User)
    {
        var platform = Detect().Platform;
        var raw = platform switch
        {
            PlatformType.Windows => level == InstallLevel.System ? _config.SystemInstallPaths.Windows : _config.DefaultInstallPaths.Windows,
            PlatformType.MacOS => level == InstallLevel.System ? _config.SystemInstallPaths.MacOS : _config.DefaultInstallPaths.MacOS,
            PlatformType.Linux => level == InstallLevel.System ? _config.SystemInstallPaths.Linux : _config.DefaultInstallPaths.Linux,
            _ => throw new PlatformNotSupportedException()
        };

        return ExpandPath(raw);
    }

    private static string ExpandPath(string path)
    {
        // Expand ~ to home directory
        if (path.StartsWith('~'))
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path[2..]);

        // Expand environment variables (%VAR%)
        return Environment.ExpandEnvironmentVariables(path);
    }
}
