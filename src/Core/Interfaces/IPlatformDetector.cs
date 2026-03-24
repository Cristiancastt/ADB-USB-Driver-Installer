using AdbDriverInstaller.Core.Enums;
using AdbDriverInstaller.Core.Models;

namespace AdbDriverInstaller.Core.Interfaces;

public interface IPlatformDetector
{
    PlatformInfo Detect();
    string GetDefaultInstallPath(InstallLevel level = InstallLevel.User);
}
