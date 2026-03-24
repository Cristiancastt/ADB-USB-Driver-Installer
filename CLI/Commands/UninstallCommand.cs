using System.ComponentModel;
using AdbDriverInstaller.CLI.Infrastructure;
using AdbDriverInstaller.CLI.Localization;
using AdbDriverInstaller.Core.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AdbDriverInstaller.CLI.Commands;

public sealed class UninstallCommand(
    IPlatformDetector platformDetector,
    IEnvironmentConfigurer environmentConfigurer)
    : AsyncCommand<UninstallCommand.Settings>
{
    private static Localizer S => Localizer.Instance;

    public sealed class Settings : CommandSettings
    {
        [CommandOption("-p|--path <PATH>")]
        [Description("Path to the platform-tools installation to remove")]
        public string? InstallPath { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            return await RunUninstallAsync(settings);
        }
        catch (UnauthorizedAccessException ex)
        {
            RenderError(S["PermissionError"], ex.Message);
            var logPath = CrashLogger.WriteLog(ex, "uninstall");
            AnsiConsole.MarkupLine($"  [dim]Log: {Markup.Escape(logPath)}[/]");
            return 1;
        }
        catch (IOException ex)
        {
            RenderError(S["FileSystemError"], ex.Message);
            var logPath = CrashLogger.WriteLog(ex, "uninstall");
            AnsiConsole.MarkupLine($"  [dim]Log: {Markup.Escape(logPath)}[/]");
            return 1;
        }
        catch (Exception ex)
        {
            RenderError(S["UnexpectedError"], ex.Message);
            var logPath = CrashLogger.WriteLog(ex, "uninstall");
            AnsiConsole.MarkupLine($"  [dim]Log: {Markup.Escape(logPath)}[/]");
            return 1;
        }
    }

    private async Task<int> RunUninstallAsync(Settings settings)
    {
        var installPath = settings.InstallPath ?? platformDetector.GetDefaultInstallPath();

        AnsiConsole.Write(new Rule($"[bold red]ADB Uninstall[/]").LeftJustified().RuleStyle("grey"));
        AnsiConsole.MarkupLine($"  [dim]{S["UninstallTitle"]}[/]");
        AnsiConsole.WriteLine();

        if (!Directory.Exists(installPath))
        {
            AnsiConsole.MarkupLine($"  [yellow]{S["InstallNotFoundAt"]}[/] [cyan]{Markup.Escape(installPath)}[/]");
            return 0;
        }

        AnsiConsole.MarkupLine($"  [bold]{S["ThisWillRemove"]}[/] [cyan]{Markup.Escape(installPath)}[/]");
        AnsiConsole.WriteLine();

        if (!AnsiConsole.Confirm($"  {S["ConfirmUninstall"]}", defaultValue: false))
        {
            AnsiConsole.MarkupLine($"  [yellow]{S["UninstallCancelled"]}[/]");
            return 0;
        }

        AnsiConsole.WriteLine();

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("red"))
            .StartAsync(S["Removing"], async _ =>
            {
                if (Directory.Exists(installPath))
                    Directory.Delete(installPath, recursive: true);

                await Task.CompletedTask;
            });

        AnsiConsole.MarkupLine($"  [green]{S["RemovedSuccess"]}[/]");

        if (environmentConfigurer.IsInPath(installPath))
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"  [yellow]{S["PathStillReferences"]}[/]");
            AnsiConsole.MarkupLine($"  [yellow]{S["ManuallyRemovePath"]}[/]");
        }

        return 0;
    }

    private static void RenderError(string title, string detail)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Panel(new Markup($"[red]{Markup.Escape(detail)}[/]"))
            .Header($"[red bold] {Markup.Escape(title)} [/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Red)
            .Padding(1, 0)
            .Expand());
    }
}
