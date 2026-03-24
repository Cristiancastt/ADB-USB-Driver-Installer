using System.IO.Compression;
using AdbDriverInstaller.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AdbDriverInstaller.Infrastructure.Services;

public sealed class FileExtractor(ILogger<FileExtractor> logger) : IFileExtractor
{
    public async Task<string> ExtractZipAsync(string zipPath, string destinationDir, IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        logger.LogInformation("Extracting {ZipPath} to {Destination}", zipPath, destinationDir);
        Directory.CreateDirectory(destinationDir);

        await Task.Run(() =>
        {
            using var archive = ZipFile.OpenRead(zipPath);
            var totalEntries = archive.Entries.Count;
            var extracted = 0;

            foreach (var entry in archive.Entries)
            {
                ct.ThrowIfCancellationRequested();

                var destinationPath = Path.GetFullPath(Path.Combine(destinationDir, entry.FullName));

                // Zip slip protection
                if (!destinationPath.StartsWith(Path.GetFullPath(destinationDir), StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning("Skipping entry with suspicious path: {Entry}", entry.FullName);
                    continue;
                }

                if (string.IsNullOrEmpty(entry.Name))
                {
                    Directory.CreateDirectory(destinationPath);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }

                extracted++;
                progress?.Report((double)extracted / totalEntries * 100);
            }
        }, ct);

        // Platform tools are typically inside a "platform-tools" subfolder in the zip
        var platformToolsDir = Path.Combine(destinationDir, "platform-tools");
        if (Directory.Exists(platformToolsDir))
            return platformToolsDir;

        logger.LogInformation("Extraction complete to {Destination}", destinationDir);
        return destinationDir;
    }
}
