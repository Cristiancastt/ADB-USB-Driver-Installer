using AdbDriverInstaller.CLI.Commands;
using AdbDriverInstaller.CLI.Infrastructure;
using AdbDriverInstaller.CLI.Localization;
using AdbDriverInstaller.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Reflection;
using System.Text;

// Ensure UTF-8 so Unicode symbols (✓ ✗ —) render correctly on all terminals
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

// Initialize localization (auto-detects system language)
_ = new Localizer();

using var settingsStream = typeof(Program).Assembly.GetManifestResourceStream("AdbDriverInstaller.CLI.appsettings.json")!;
var configuration = new ConfigurationBuilder().AddJsonStream(settingsStream).Build();

var services = new ServiceCollection();

services.AddLogging(builder =>
{
    builder.SetMinimumLevel(LogLevel.Warning);
    builder.AddConsole();
});

services.AddInfrastructure(configuration);

var version = typeof(Program).Assembly
    .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()
    ?.InformationalVersion?.Split('+')[0] ?? "dev";

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.SetApplicationName("adb-installer");
    config.SetApplicationVersion(version);

    config.AddCommand<InstallCommand>("install").WithDescription("Interactive wizard to install ADB, Fastboot & USB drivers");

    config.AddCommand<VerifyCommand>("verify").WithDescription("Verify that ADB and Fastboot are properly installed");

    config.AddCommand<UninstallCommand>("uninstall").WithDescription("Remove installed platform tools");

    config.AddCommand<UpdateCommand>("update").WithDescription("Update ADB and Fastboot to the latest version");
});

// If no args, default to the interactive install wizard
if (args.Length == 0) args = ["install"];

try
{
    return await app.RunAsync(args);
}
catch (Exception ex)
{
    var logPath = CrashLogger.WriteLog(ex, string.Join(' ', args));
    AnsiConsole.WriteLine();
    AnsiConsole.Write(new Panel(new Markup($"[{Theme.Red}]{Markup.Escape(ex.Message)}[/]"))
        .Header($"[{Theme.Red} bold] Fatal Error [/]")
        .Border(BoxBorder.Rounded)
        .BorderColor(Theme.RedColor)
        .Padding(1, 0)
        .Expand());
    AnsiConsole.MarkupLine($"  [{Theme.Dim}]Log: {Markup.Escape(logPath)}[/]");
    return 1;
}
