using AdbDriverInstaller.Core.Models;

namespace AdbDriverInstaller.Core.Interfaces;

public interface IAdbVerifier
{
    Task<VerificationResult> VerifyAsync(string? platformToolsPath = null, CancellationToken ct = default);
}
