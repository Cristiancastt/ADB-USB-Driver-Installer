using AdbDriverInstaller.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace AdbDriverInstaller.Infrastructure.Services;

public sealed class DownloadService(IHttpClientFactory httpClientFactory, ILogger<DownloadService> logger)
    : IDownloadService
{
    private const int MaxRetries = 3;

    public async Task<string> DownloadFileAsync(string url, string destinationDir, IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(destinationDir);

        var uri = new Uri(url);
        var fileName = Path.GetFileName(uri.LocalPath);
        var filePath = Path.Combine(destinationDir, fileName);

        logger.LogInformation("Downloading {Url} to {FilePath}", url, filePath);

        for (var attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                using var client = httpClientFactory.CreateClient("AdbInstaller");
                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;

                await using var contentStream = await response.Content.ReadAsStreamAsync(ct);
                await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None,
                    bufferSize: 81920, useAsync: true);

                var buffer = new byte[81920];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                    totalRead += bytesRead;

                    if (totalBytes > 0)
                        progress?.Report((double)totalRead / totalBytes * 100);
                }

                progress?.Report(100);
                logger.LogInformation("Download complete: {FilePath} ({Bytes} bytes)", filePath, totalRead);
                return filePath;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                logger.LogWarning(ex, "Download attempt {Attempt}/{Max} failed for {Url}. Retrying...",
                    attempt, MaxRetries, url);
                progress?.Report(0);
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2), ct);
            }
        }

        // Final attempt — let exceptions propagate
        using var finalClient = httpClientFactory.CreateClient("AdbInstaller");
        using var finalResponse = await finalClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        finalResponse.EnsureSuccessStatusCode();

        var finalTotalBytes = finalResponse.Content.Headers.ContentLength ?? -1L;

        await using var finalContentStream = await finalResponse.Content.ReadAsStreamAsync(ct);
        await using var finalFileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 81920, useAsync: true);

        var finalBuffer = new byte[81920];
        long finalTotalRead = 0;
        int finalBytesRead;

        while ((finalBytesRead = await finalContentStream.ReadAsync(finalBuffer, ct)) > 0)
        {
            await finalFileStream.WriteAsync(finalBuffer.AsMemory(0, finalBytesRead), ct);
            finalTotalRead += finalBytesRead;

            if (finalTotalBytes > 0)
                progress?.Report((double)finalTotalRead / finalTotalBytes * 100);
        }

        progress?.Report(100);
        logger.LogInformation("Download complete: {FilePath} ({Bytes} bytes)", filePath, finalTotalRead);
        return filePath;
    }
}
