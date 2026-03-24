using AdbDriverInstaller.Core.Enums;

namespace AdbDriverInstaller.Core.Models;

public sealed record PlatformInfo(
    PlatformType Platform,
    string PlatformToolsUrl,
    string? UsbDriverUrl = null);
