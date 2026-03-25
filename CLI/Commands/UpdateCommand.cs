using AdbDriverInstaller.CLI.Infrastructure;
using AdbDriverInstaller.CLI.Localization;
using AdbDriverInstaller.Core.Enums;
using AdbDriverInstaller.Core.Interfaces;
using AdbDriverInstaller.Core.Models;
using AdbDriverInstaller.Infrastructure.Services;
using Spectre.Console;
using Spectre.Console.Cli;

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
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine($"  [yellow]{S["InstallCancelled"]}[/]");
            return 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Panel(new Markup($"[red]{Markup.Escape(ex.Message)}[/]"))
                .Header($"[red bold] {S["UnexpectedError"]} [/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Red)
                .Padding(1, 0)
                .Expand());
            var logPath = CrashLogger.WriteLog(ex, "update");
            AnsiConsole.MarkupLine($"  [dim]Log: {Markup.Escape(logPath)}[/]");
            return 1;
        }
    }

    private async Task<int> RunUpdateAsync()
    {
        AnsiConsole.Write(new Rule($"[bold dodgerblue1]{S["UpdateTitle"]}[/]").LeftJustified().RuleStyle("grey"));
        AnsiConsole.MarkupLine($"  [dim]{S["UpdateSubtitle"]}[/]");
        AnsiConsole.WriteLine();

        // 1. Find current installation
        var defaultPath = platformDetector.GetDefaultInstallPath();
        var current = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots2)
            .SpinnerStyle(Style.Parse("dodgerblue1"))
            .StartAsync(S["CheckingInstallation"],
                async _ => await adbVerifier.VerifyAsync(defaultPath));

        if (!current.AdbFound)
        {
            AnsiConsole.MarkupLine($"  [yellow]{S["NoExistingInstall"]}[/]");
            AnsiConsole.MarkupLine($"  [dim]{S["RunInstallHint"]}[/]");
            return 1;
        }

        var installPath = current.AdbPath is not null
            ? Path.GetDirectoryName(current.AdbPath)!
            : defaultPath;

        // 2. Show current version
        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey);
        table.AddColumn(new TableColumn($"[bold]{S["Component"]}[/]").PadRight(2));
        table.AddColumn($"[bold]{S["CurrentVersion"]}[/]");
        table.AddRow("ADB", $"[cyan]{Markup.Escape(current.AdbVersion ?? "unknown")}[/]");
        table.AddRow("Fastboot", $"[cyan]{Markup.Escape(current.FastbootVersion ?? "unknown")}[/]");
        table.AddRow(S["InstallPath"], $"[dim]{Markup.Escape(installPath)}[/]");
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine($"  [dim]{S["NoVersionApi"]}[/]");
        AnsiConsole.WriteLine();

        if (!AnsiConsole.Confirm($"[bold]{S["ConfirmUpdate"]}[/]"))
        {
            AnsiConsole.MarkupLine($"  [yellow]{S["InstallCancelled"]}[/]");
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
                new PercentageColumn(),
                new RemainingTimeColumn())
            .StartAsync(async ctx =>
            {
                var dlTask = ctx.AddTask($"[green]{S["DownloadingPlatformTools"]}[/]", maxValue: 100);
                var exTask = ctx.AddTask($"[blue]{S["ExtractingFiles"]}[/]", maxValue: 100);
                exTask.IsIndeterminate = true;
                var verTask = ctx.AddTask($"[cyan]{S["VerifyingInstallation"]}[/]", autoStart: false, maxValue: 100);
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

            grid.AddRow($"[dim]{S["PreviousVersion"]}[/]", $"[grey]{Markup.Escape(current.AdbVersion ?? "unknown")}[/]");
            grid.AddRow($"[dim]{S["UpdatedVersion"]}[/]", $"[green]{Markup.Escape(result.AdbVersion ?? "latest")}[/]");
            grid.AddRow($"[dim]{S["Path"]}:[/]", $"[cyan]{Markup.Escape(result.PlatformToolsPath ?? installPath)}[/]");

            AnsiConsole.Write(new Panel(grid)
                .Header($"[green bold] {S["UpdateSuccessful"]} [/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Green)
                .Padding(1, 0)
                .Expand());

            return 0;
        }

        AnsiConsole.Write(new Panel(new Markup($"[red]{Markup.Escape(result.ErrorMessage ?? S["UnknownError"])}[/]"))
            .Header($"[red bold] {S["InstallFailed"]} [/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Red)
            .Padding(1, 0)
            .Expand());

        return 1;
    }
}
