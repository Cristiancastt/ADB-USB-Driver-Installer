using AdbDriverInstaller.Core.Enums;

namespace AdbDriverInstaller.Core.Models;

public sealed class InstallOptions
{
    public string? InstallPath { get; init; }
    public bool AddToPath { get; init; } = true;
    public bool InstallUsbDrivers { get; init; } = true;
    public bool VerifyAfterInstall { get; init; } = true;
    public InstallLevel Level { get; init; } = InstallLevel.User;
}
