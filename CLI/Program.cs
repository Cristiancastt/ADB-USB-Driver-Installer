using AdbDriverInstaller.CLI.Commands;
using AdbDriverInstaller.CLI.Infrastructure;
using AdbDriverInstaller.CLI.Localization;
using AdbDriverInstaller.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

// Initialize localization (auto-detects system language)
_ = new Localizer();

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

var services = new ServiceCollection();

services.AddLogging(builder =>
{
    builder.SetMinimumLevel(LogLevel.Warning);
    builder.AddConsole();
});

services.AddInfrastructure(configuration);

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.SetApplicationName("adb-installer");
    config.SetApplicationVersion("1.0.0");

    config.AddCommand<InstallCommand>("install")
        .WithDescription("Interactive wizard to install ADB, Fastboot & USB drivers");

    config.AddCommand<VerifyCommand>("verify")
        .WithDescription("Verify that ADB and Fastboot are properly installed");

    config.AddCommand<UninstallCommand>("uninstall")
        .WithDescription("Remove installed platform tools");
});

// If no args, default to the interactive install wizard
if (args.Length == 0)
    args = ["install"];

return await app.RunAsync(args);
