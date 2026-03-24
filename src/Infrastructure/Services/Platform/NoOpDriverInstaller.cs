using AdbDriverInstaller.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AdbDriverInstaller.Infrastructure.Services.Platform;

/// <summary>
/// No-op driver installer for macOS and Linux where USB drivers are handled by the kernel.
/// </summary>
public sealed class NoOpDriverInstaller(ILogger<NoOpDriverInstaller> logger) : IDriverInstaller
{
    public bool IsSupported => false;

    public Task<bool> InstallUsbDriversAsync(string driverPath, CancellationToken ct = default)
    {
        logger.LogInformation("USB driver installation is not required on this platform. The kernel handles ADB device access natively.");
        return Task.FromResult(true);
    }
}
