using AdbDriverInstaller.Core.Configuration;
using AdbDriverInstaller.Core.Interfaces;
using AdbDriverInstaller.Infrastructure.Services;
using AdbDriverInstaller.Infrastructure.Services.Platform;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AdbDriverInstaller.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.Configure<AdbInstallerConfig>(configuration.GetSection("AdbInstaller"));

        // HTTP client
        services.AddHttpClient("AdbInstaller", client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ADB-Driver-Installer/1.0");
        });

        // Core services
        services.AddSingleton<IPlatformDetector, PlatformDetector>();
        services.AddSingleton<IDownloadService, DownloadService>();
        services.AddSingleton<IFileExtractor, FileExtractor>();
        services.AddSingleton<IAdbVerifier, AdbVerifier>();
        services.AddSingleton<UnixPermissionHelper>();

        // Platform-specific services
        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<IEnvironmentConfigurer, WindowsEnvironmentConfigurer>();
            services.AddSingleton<IDriverInstaller, WindowsDriverInstaller>();
        }
        else
        {
            services.AddSingleton<IEnvironmentConfigurer, UnixEnvironmentConfigurer>();
            services.AddSingleton<IDriverInstaller, NoOpDriverInstaller>();
        }

        // Orchestrator
        services.AddSingleton<IInstallOrchestrator, InstallOrchestrator>();
        services.AddSingleton<InstallOrchestrator>();

        return services;
    }
}
