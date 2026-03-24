using AdbDriverInstaller.Core.Models;

namespace AdbDriverInstaller.Core.Interfaces;

public interface IInstallOrchestrator
{
    Task<InstallResult> InstallAsync(InstallOptions options, CancellationToken ct = default);
}
