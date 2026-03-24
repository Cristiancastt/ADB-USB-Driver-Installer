namespace AdbDriverInstaller.Core.Configuration;

public sealed class AdbInstallerConfig
{
    public string BaseUrl { get; set; } = "https://dl.google.com/android/repository";

    public PlatformToolsConfig PlatformTools { get; set; } = new();
    public UsbDriverConfig UsbDriver { get; set; } = new();
    public DefaultPathsConfig DefaultInstallPaths { get; set; } = new();
    public SystemPathsConfig SystemInstallPaths { get; set; } = new();
}

public sealed class PlatformToolsConfig
{
    public string Windows { get; set; } = "platform-tools-latest-windows.zip";
    public string Linux { get; set; } = "platform-tools-latest-linux.zip";
    public string MacOS { get; set; } = "platform-tools-latest-darwin.zip";
}

public sealed class UsbDriverConfig
{
    public string? Windows { get; set; } = "usb_driver_r13-windows.zip";
}

public sealed class DefaultPathsConfig
{
    public string Windows { get; set; } = @"%LOCALAPPDATA%\Android\platform-tools";
    public string Linux { get; set; } = "~/.android/platform-tools";
    public string MacOS { get; set; } = "~/Library/Android/platform-tools";
}

public sealed class SystemPathsConfig
{
    public string Windows { get; set; } = @"C:\Android\platform-tools";
    public string Linux { get; set; } = "/opt/android/platform-tools";
    public string MacOS { get; set; } = "/opt/android/platform-tools";
}
