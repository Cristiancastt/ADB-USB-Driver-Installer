namespace AdbDriverInstaller.Core.Models;

public sealed class InstallResult
{
    public bool Success { get; init; }
    public string? PlatformToolsPath { get; init; }
    public string? AdbVersion { get; init; }
    public string? FastbootVersion { get; init; }
    public bool PathConfigured { get; init; }
    public bool UsbDriversInstalled { get; init; }
    public string? ErrorMessage { get; init; }

    public static InstallResult Ok(string platformToolsPath, string? adbVersion = null, string? fastbootVersion = null,
        bool pathConfigured = false, bool usbDriversInstalled = false) =>
        new()
        {
            Success = true,
            PlatformToolsPath = platformToolsPath,
            AdbVersion = adbVersion,
            FastbootVersion = fastbootVersion,
            PathConfigured = pathConfigured,
            UsbDriversInstalled = usbDriversInstalled
        };

    public static InstallResult Fail(string error) =>
        new() { Success = false, ErrorMessage = error };
}
