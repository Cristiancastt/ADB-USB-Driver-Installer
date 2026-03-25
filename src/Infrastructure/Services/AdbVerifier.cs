using AdbDriverInstaller.Core.Interfaces;
using AdbDriverInstaller.Core.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AdbDriverInstaller.Infrastructure.Services;

public sealed class AdbVerifier(ILogger<AdbVerifier> logger) : IAdbVerifier
{
    public async Task<VerificationResult> VerifyAsync(string? platformToolsPath = null, CancellationToken ct = default)
    {
        var adbName = OperatingSystem.IsWindows() ? "adb.exe" : "adb";
        var fastbootName = OperatingSystem.IsWindows() ? "fastboot.exe" : "fastboot";

        string? adbPath = null;
        string? fastbootPath = null;

        // Check in specified directory first
        if (!string.IsNullOrWhiteSpace(platformToolsPath))
        {
            var adbCandidate = Path.Combine(platformToolsPath, adbName);
            var fastbootCandidate = Path.Combine(platformToolsPath, fastbootName);

            if (File.Exists(adbCandidate)) adbPath = adbCandidate;
            if (File.Exists(fastbootCandidate)) fastbootPath = fastbootCandidate;
        }

        // If not found, try PATH
        adbPath ??= FindInPath(adbName);
        fastbootPath ??= FindInPath(fastbootName);

        var adbVersion = adbPath is not null ? await GetVersionAsync(adbPath, ct) : null;
        var fastbootVersion = fastbootPath is not null ? await GetVersionAsync(fastbootPath, ct) : null;

        var isInPath = IsCommandInPath(adbName);

        logger.LogInformation("ADB: {AdbPath} ({AdbVersion}), Fastboot: {FastbootPath} ({FastbootVersion}), InPath: {InPath}",
            adbPath, adbVersion, fastbootPath, fastbootVersion, isInPath);

        return new VerificationResult(
            AdbFound: adbPath is not null,
            FastbootFound: fastbootPath is not null,
            AdbVersion: adbVersion,
            FastbootVersion: fastbootVersion,
            AdbPath: adbPath,
            FastbootPath: fastbootPath,
            IsInPath: isInPath);
    }

    private static string? FindInPath(string executableName)
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var separator = OperatingSystem.IsWindows() ? ';' : ':';

        foreach (var dir in pathVar.Split(separator, StringSplitOptions.RemoveEmptyEntries))
        {
            var fullPath = Path.Combine(dir.Trim(), executableName);
            if (File.Exists(fullPath))
                return fullPath;
        }

        return null;
    }

    private static bool IsCommandInPath(string executableName) => FindInPath(executableName) is not null;

    private async Task<string?> GetVersionAsync(string executablePath, CancellationToken ct)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = "version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    // Prevent adb from starting a server that blocks forever
                    EnvironmentVariables = { ["ADB_TRACE"] = "" }
                }
            };

            process.Start();

            // Use a combined timeout: either the CancellationToken fires or 10s passes
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            string output;
            try
            {
                output = await process.StandardOutput.ReadToEndAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                KillProcess(process);
                return null;
            }

            // Wait for exit with timeout — don't hang forever
            if (!process.WaitForExit(5000))
            {
                KillProcess(process);
                logger.LogWarning("{Executable} did not exit in time, killed", executablePath);
            }

            var versionLine = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return versionLine?.Trim();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get version for {Executable}", executablePath);
            return null;
        }
    }

    private static void KillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch { }
    }
}
