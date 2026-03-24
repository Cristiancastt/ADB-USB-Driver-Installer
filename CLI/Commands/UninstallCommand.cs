using System.ComponentModel;
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
            AnsiConsole.MarkupLine($"[red]{S["PermissionError"]}:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
        catch (IOException ex)
        {
            AnsiConsole.MarkupLine($"[red]{S["FileSystemError"]}:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]{S["UnexpectedError"]}:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    private async Task<int> RunUninstallAsync(Settings settings)
    {
        var installPath = settings.InstallPath ?? platformDetector.GetDefaultInstallPath();

        AnsiConsole.Write(new Rule($"[bold red]{S["UninstallTitle"]}[/]").LeftJustified());
        AnsiConsole.WriteLine();

        if (!Directory.Exists(installPath))
        {
            AnsiConsole.MarkupLine($"[yellow]{S["InstallNotFoundAt"]}[/] [cyan]{Markup.Escape(installPath)}[/]");
            return 0;
        }

        AnsiConsole.MarkupLine($"[bold]{S["ThisWillRemove"]}[/] [cyan]{Markup.Escape(installPath)}[/]");

        if (!AnsiConsole.Confirm(S["ConfirmUninstall"], defaultValue: false))
        {
            AnsiConsole.MarkupLine($"[yellow]{S["UninstallCancelled"]}[/]");
            return 0;
        }

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("red"))
            .StartAsync(S["Removing"], async _ =>
            {
                if (Directory.Exists(installPath))
                    Directory.Delete(installPath, recursive: true);

                await Task.CompletedTask;
            });

        AnsiConsole.MarkupLine($"[green]{S["RemovedSuccess"]}[/]");

        if (environmentConfigurer.IsInPath(installPath))
        {
            AnsiConsole.MarkupLine($"[yellow]{S["PathStillReferences"]}[/]");
            AnsiConsole.MarkupLine($"[yellow]{S["ManuallyRemovePath"]}[/]");
        }

        return 0;
    }
}
