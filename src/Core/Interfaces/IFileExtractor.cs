namespace AdbDriverInstaller.Core.Interfaces;

public interface IFileExtractor
{
    Task<string> ExtractZipAsync(string zipPath, string destinationDir, IProgress<double>? progress = null, CancellationToken ct = default);
}
