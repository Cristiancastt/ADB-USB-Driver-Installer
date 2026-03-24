namespace AdbDriverInstaller.Core.Interfaces;

public interface IDriverInstaller
{
    bool IsSupported { get; }
    Task<bool> InstallUsbDriversAsync(string driverPath, CancellationToken ct = default);
}
