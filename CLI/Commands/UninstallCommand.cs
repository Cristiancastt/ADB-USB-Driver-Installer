using AdbDriverInstaller.CLI.Infrastructure;
using AdbDriverInstaller.CLI.Localization;
using AdbDriverInstaller.Core.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

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
            AnsiConsole.MarkupLine($"  [{Theme.Dim}]Log: {Markup.Escape(logPath)}[/]");
            return 1;
        }
        catch (IOException ex)
        {
            RenderError(S["FileSystemError"], ex.Message);
            var logPath = CrashLogger.WriteLog(ex, "uninstall");
            AnsiConsole.MarkupLine($"  [{Theme.Dim}]Log: {Markup.Escape(logPath)}[/]");
            return 1;
        }
        catch (Exception ex)
        {
            RenderError(S["UnexpectedError"], ex.Message);
            var logPath = CrashLogger.WriteLog(ex, "uninstall");
            AnsiConsole.MarkupLine($"  [{Theme.Dim}]Log: {Markup.Escape(logPath)}[/]");
            return 1;
        }
    }

    private async Task<int> RunUninstallAsync(Settings settings)
    {
        var installPath = settings.InstallPath ?? platformDetector.GetDefaultInstallPath();

        AnsiConsole.Write(new Rule($"[bold {Theme.Red}]{S["UninstallHeader"]}[/]").LeftJustified().RuleStyle(Theme.Gray));
        AnsiConsole.MarkupLine($"  [{Theme.Dim}]{S["UninstallTitle"]}[/]");
        AnsiConsole.WriteLine();

        if (!Directory.Exists(installPath))
        {
            AnsiConsole.MarkupLine($"  [{Theme.Amber}]{S["InstallNotFoundAt"]}[/] [{Theme.Cyan}]{Markup.Escape(installPath)}[/]");
            return 0;
        }

        AnsiConsole.MarkupLine($"  [bold]{S["ThisWillRemove"]}[/] [{Theme.Cyan}]{Markup.Escape(installPath)}[/]");
        AnsiConsole.WriteLine();

        if (!AnsiConsole.Confirm($"  {S["ConfirmUninstall"]}", defaultValue: false))
        {
            AnsiConsole.MarkupLine($"  [{Theme.Amber}]{S["UninstallCancelled"]}[/]");
            return 0;
        }

        AnsiConsole.WriteLine();

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse(Theme.Red))
            .StartAsync(S["Removing"], async _ =>
            {
                if (Directory.Exists(installPath))
                    Directory.Delete(installPath, recursive: true);

                await Task.CompletedTask;
            });

        AnsiConsole.MarkupLine($"  [{Theme.Green}]{Theme.Ok} {S["RemovedSuccess"]}[/]");

        if (environmentConfigurer.IsInPath(installPath))
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"  [{Theme.Amber}]{S["PathStillReferences"]}[/]");
            AnsiConsole.MarkupLine($"  [{Theme.Amber}]{S["ManuallyRemovePath"]}[/]");
        }

        return 0;
    }

    private static void RenderError(string title, string detail)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Panel(new Markup($"[{Theme.Red}]{Markup.Escape(detail)}[/]"))
            .Header($"[{Theme.Red} bold] {Markup.Escape(title)} [/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Theme.RedColor)
            .Padding(1, 0)
            .Expand());
    }
}
