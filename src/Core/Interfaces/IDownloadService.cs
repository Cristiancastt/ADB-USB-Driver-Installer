namespace AdbDriverInstaller.Core.Interfaces;

public interface IDownloadService
{
    Task<string> DownloadFileAsync(string url, string destinationDir, IProgress<double>? progress = null, CancellationToken ct = default);
}
