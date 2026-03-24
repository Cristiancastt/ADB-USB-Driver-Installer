using AdbDriverInstaller.Core.Enums;

namespace AdbDriverInstaller.Core.Interfaces;

public interface IEnvironmentConfigurer
{
    Task<bool> AddToPathAsync(string directoryPath, InstallLevel level = InstallLevel.User, CancellationToken ct = default);
    bool IsInPath(string directoryPath);
}
