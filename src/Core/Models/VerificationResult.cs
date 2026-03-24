namespace AdbDriverInstaller.Core.Models;

public sealed record VerificationResult(
    bool AdbFound,
    bool FastbootFound,
    string? AdbVersion,
    string? FastbootVersion,
    string? AdbPath,
    string? FastbootPath,
    bool IsInPath);
