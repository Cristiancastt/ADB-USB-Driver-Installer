using AdbDriverInstaller.CLI.Infrastructure;
using AdbDriverInstaller.CLI.Localization;
using AdbDriverInstaller.Core.Enums;
using AdbDriverInstaller.Core.Interfaces;
using AdbDriverInstaller.Core.Models;
using AdbDriverInstaller.Infrastructure.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Http;

namespace AdbDriverInstaller.CLI.Commands;

public sealed class UpdateCommand(
    IPlatformDetector platformDetector,
    IAdbVerifier adbVerifier,
    InstallOrchestrator orchestrator) : AsyncCommand
{
    private static Localizer S => Localizer.Instance;

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        try
        {
            return await RunUpdateAsync();
        }
        catch (TaskCanceledException)
        {
            AnsiConsole.MarkupLine($"  [{Theme.Amber}]{S["OperationTimedOut"]}[/]");
            return 1;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine($"  [{Theme.Amber}]{S["OperationCancelled"]}[/]");
            return 1;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Panel(new Markup($"[{Theme.Red}]{Markup.Escape(ex.Message)}[/]"))
                .Header($"[{Theme.Red} bold] {S["NetworkError"]} [/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Theme.RedColor)
                .Padding(1, 0)
                .Expand());
            var logPath = CrashLogger.WriteLog(ex, "update");
            AnsiConsole.MarkupLine($"  [{Theme.Dim}]Log: {Markup.Escape(logPath)}[/]");
            return 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Panel(new Markup($"[{Theme.Red}]{Markup.Escape(ex.Message)}[/]"))
                .Header($"[{Theme.Red} bold] {S["UnexpectedError"]} [/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Theme.RedColor)
                .Padding(1, 0)
                .Expand());
            var logPath = CrashLogger.WriteLog(ex, "update");
            AnsiConsole.MarkupLine($"  [{Theme.Dim}]Log: {Markup.Escape(logPath)}[/]");
            return 1;
        }
    }

    private async Task<int> RunUpdateAsync()
    {
        AnsiConsole.Write(new Rule($"[bold {Theme.Blue}]{S["UpdateTitle"]}[/]").LeftJustified().RuleStyle(Theme.Gray));
        AnsiConsole.MarkupLine($"  [{Theme.Dim}]{S["UpdateSubtitle"]}[/]");
        AnsiConsole.WriteLine();

        // 1. Find current installation
        var defaultPath = platformDetector.GetDefaultInstallPath();
        var current = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots2)
            .SpinnerStyle(Style.Parse(Theme.Blue))
            .StartAsync(S["CheckingInstallation"],
                async _ => await adbVerifier.VerifyAsync(defaultPath));

        if (!current.AdbFound)
        {
            AnsiConsole.MarkupLine($"  [{Theme.Amber}]{S["NoExistingInstall"]}[/]");
            AnsiConsole.MarkupLine($"  [{Theme.Dim}]{S["RunInstallHint"]}[/]");
            return 1;
        }

        var installPath = current.AdbPath is not null
            ? Path.GetDirectoryName(current.AdbPath)!
            : defaultPath;

        // 2. Show current version
        var table = new Table().Border(TableBorder.Rounded).BorderColor(Theme.GrayColor);
        table.AddColumn(new TableColumn($"[bold]{S["Component"]}[/]").PadRight(2));
        table.AddColumn($"[bold]{S["CurrentVersion"]}[/]");
        table.AddRow("ADB", $"[{Theme.Cyan}]{Markup.Escape(current.AdbVersion ?? "unknown")}[/]");
        table.AddRow("Fastboot", $"[{Theme.Cyan}]{Markup.Escape(current.FastbootVersion ?? "unknown")}[/]");
        table.AddRow(S["InstallPath"], $"[{Theme.Dim}]{Markup.Escape(installPath)}[/]");
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine($"  [{Theme.Dim}]{S["NoVersionApi"]}[/]");
        AnsiConsole.WriteLine();

        if (!AnsiConsole.Confirm($"[bold]{S["ConfirmUpdate"]}[/]"))
        {
            AnsiConsole.MarkupLine($"  [{Theme.Amber}]{S["InstallCancelled"]}[/]");
            return 0;
        }

        AnsiConsole.WriteLine();

        // 3. Re-install with same options
        var options = new InstallOptions
        {
            InstallPath = installPath,
            AddToPath = false,       // PATH already configured
            InstallUsbDrivers = false, // Drivers already installed
            VerifyAfterInstall = true,
            Level = InstallLevel.User
        };

        var result = await AnsiConsole.Progress()
            .AutoRefresh(true)
            .HideCompleted(false)
            .Columns(
                new SpinnerColumn(),
                new TaskDescriptionColumn { Alignment = Justify.Left },
                new ProgressBarColumn(),
                new PercentageColumn())
            .StartAsync(async ctx =>
            {
                var dlTask = ctx.AddTask($"[{Theme.Green}]{S["DownloadingPlatformTools"]}[/]", maxValue: 100);
                var exTask = ctx.AddTask($"[{Theme.Blue}]{S["ExtractingFiles"]}[/]", maxValue: 100);
                exTask.IsIndeterminate = true;
                var verTask = ctx.AddTask($"[{Theme.Blue}]{S["VerifyingInstallation"]}[/]", autoStart: false, maxValue: 100);
                verTask.IsIndeterminate = true;

                orchestrator.DownloadProgress = new Progress<double>(v =>
                {
                    dlTask.Value = Math.Min(v, 100);
                    if (v >= 100 && !dlTask.IsFinished) dlTask.StopTask();
                });

                orchestrator.ExtractProgress = new Progress<double>(v =>
                {
                    exTask.IsIndeterminate = false;
                    exTask.Value = Math.Min(v, 100);
                    if (v >= 100 && !exTask.IsFinished) exTask.StopTask();
                });

                orchestrator.OnStatusUpdate = msg =>
                {
                    if (msg.Contains("Verifying", StringComparison.OrdinalIgnoreCase))
                        verTask.StartTask();
                };

                var installResult = await orchestrator.InstallAsync(options);

                if (verTask is { IsFinished: false })
                {
                    verTask.IsIndeterminate = false;
                    verTask.Value = verTask.MaxValue;
                    verTask.StopTask();
                }

                return installResult;
            });

        AnsiConsole.WriteLine();

        if (result.Success)
        {
            var grid = new Grid();
            grid.AddColumn(new GridColumn().PadRight(2));
            grid.AddColumn();

            grid.AddRow($"[{Theme.Dim}]{S["PreviousVersion"]}[/]", $"[{Theme.Gray}]{Markup.Escape(current.AdbVersion ?? "unknown")}[/]");
            grid.AddRow($"[{Theme.Dim}]{S["UpdatedVersion"]}[/]", $"[{Theme.Green}]{Markup.Escape(result.AdbVersion ?? "latest")}[/]");
            grid.AddRow($"[{Theme.Dim}]{S["Path"]}:[/]", $"[{Theme.Cyan}]{Markup.Escape(result.PlatformToolsPath ?? installPath)}[/]");

            AnsiConsole.Write(new Panel(grid)
                .Header($"[{Theme.Green} bold] {Theme.Ok} {S["UpdateSuccessful"]} [/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Theme.GreenColor)
                .Padding(1, 0)
                .Expand());

            return 0;
        }

        AnsiConsole.Write(new Panel(new Markup($"[{Theme.Red}]{Markup.Escape(result.ErrorMessage ?? S["UnknownError"])}[/]"))
            .Header($"[{Theme.Red} bold] {S["InstallFailed"]} [/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Theme.RedColor)
            .Padding(1, 0)
            .Expand());

        return 1;
    }
}
