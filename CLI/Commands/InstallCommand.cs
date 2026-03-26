using AdbDriverInstaller.CLI.Infrastructure;
using AdbDriverInstaller.CLI.Localization;
using AdbDriverInstaller.Core.Enums;
using AdbDriverInstaller.Core.Interfaces;
using AdbDriverInstaller.Core.Models;
using AdbDriverInstaller.Infrastructure.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace AdbDriverInstaller.CLI.Commands;

public sealed class InstallCommand(
    IPlatformDetector platformDetector,
    IAdbVerifier adbVerifier,
    InstallOrchestrator orchestrator) : AsyncCommand<InstallCommand.Settings>
{
    private static Localizer S => Localizer.Instance;

    public sealed class Settings : CommandSettings
    {
        [CommandOption("-s|--silent")]
        [Description("Non-interactive mode. Installs with defaults, no prompts.")]
        public bool Silent { get; init; }

        [CommandOption("-p|--path <PATH>")]
        [Description("Custom install path (default: platform default)")]
        public string? InstallPath { get; init; }

        [CommandOption("--system")]
        [Description("Install system-wide (requires admin)")]
        public bool System { get; init; }

        [CommandOption("--no-drivers")]
        [Description("Skip USB driver installation (Windows)")]
        public bool NoDrivers { get; init; }

        [CommandOption("--no-path")]
        [Description("Don't add to PATH")]
        public bool NoPath { get; init; }

        [CommandOption("--no-verify")]
        [Description("Skip post-install verification")]
        public bool NoVerify { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            return settings.Silent
                ? await RunSilentAsync(settings)
                : await RunWizardAsync(settings);
        }
        catch (TaskCanceledException)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"  [{Theme.Amber}]{S["OperationTimedOut"]}[/]");
            return 1;
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"  [{Theme.Amber}]{S["OperationCancelled"]}[/]");
            return 1;
        }
        catch (HttpRequestException ex)
        {
            RenderError(S["NetworkError"], GetNetworkErrorDetail(ex));
            WriteCrashLog(ex);
            return 1;
        }
        catch (SocketException ex)
        {
            RenderError(S["NetworkError"], ex.Message);
            WriteCrashLog(ex);
            return 1;
        }
        catch (UnauthorizedAccessException ex)
        {
            RenderError(S["PermissionError"], ex.Message);
            WriteCrashLog(ex);
            return 1;
        }
        catch (IOException ex)
        {
            RenderError(S["FileSystemError"], ex.Message);
            WriteCrashLog(ex);
            return 1;
        }
        catch (Exception ex)
        {
            RenderError(S["UnexpectedError"], ex.Message);
            WriteCrashLog(ex);
            return 1;
        }
    }

    private static void WriteCrashLog(Exception ex)
    {
        var logPath = CrashLogger.WriteLog(ex, "install");
        AnsiConsole.MarkupLine($"  [{Theme.Dim}]Log: {Markup.Escape(logPath)}[/]");
    }

    private async Task<int> RunSilentAsync(Settings settings)
    {
        var level = settings.System ? InstallLevel.System : InstallLevel.User;
        var installPath = settings.InstallPath ?? platformDetector.GetDefaultInstallPath(level);

        Console.WriteLine(S["SilentModeHeader"]);
        Console.WriteLine(S.Format("SilentInstallPath", installPath));

        var options = new InstallOptions
        {
            InstallPath = installPath,
            AddToPath = !settings.NoPath,
            InstallUsbDrivers = !settings.NoDrivers,
            VerifyAfterInstall = !settings.NoVerify,
            Level = level
        };

        orchestrator.OnStatusUpdate = msg => Console.WriteLine($"  {msg}");
        orchestrator.DownloadProgress = new Progress<double>(_ => { });
        orchestrator.ExtractProgress = new Progress<double>(_ => { });

        var result = await orchestrator.InstallAsync(options);

        if (result.Success)
        {
            Console.WriteLine(S.Format("SilentOk", result.AdbVersion ?? "installed", result.PlatformToolsPath ?? ""));
            return 0;
        }

        Console.Error.WriteLine(S.Format("SilentFail", result.ErrorMessage ?? ""));
        return 1;
    }

    private async Task<int> RunWizardAsync(Settings settings)
    {
        AnsiConsole.Clear();
        RenderHeader();

        // ── Check for existing installation ─────────────────────
        var existingPath = platformDetector.GetDefaultInstallPath();

        var existing = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots2)
            .SpinnerStyle(Style.Parse(Theme.Blue))
            .StartAsync(S["CheckingExisting"],
                async _ => await adbVerifier.VerifyAsync(existingPath));

        if (existing.AdbFound)
        {
            AnsiConsole.MarkupLine($"  [{Theme.Amber}]{S["AdbAlreadyInstalled"]}[/] [{Theme.Cyan}]{Markup.Escape(existing.AdbVersion ?? "unknown")}[/]");
            if (existing.AdbPath is not null)
                AnsiConsole.MarkupLine($"  [{Theme.Dim}]Path: {Markup.Escape(existing.AdbPath)}[/]");
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[bold]{S["ExistingAction"]}[/]")
                    .AddChoices(S["ReinstallUpdate"], S["Cancel"]));

            if (choice == S["Cancel"])
            {
                AnsiConsole.MarkupLine($"  [{Theme.Amber}]{S["InstallCancelled"]}[/]");
                return 0;
            }

            AnsiConsole.WriteLine();
        }

        // ── Step 1 — Platform Detection ─────────────────────────
        WriteStep(1, S["StepDetectingPlatform"]);

        var platform = platformDetector.Detect();
        var osLabel = platform.Platform switch
        {
            PlatformType.Windows => "Windows",
            PlatformType.MacOS => "macOS",
            PlatformType.Linux => "Linux",
            _ => "Unknown"
        };

        //var arch = RuntimeInformation.OSArchitecture.ToString();
        var arch = RuntimeInformation.OSDescription.ToString() + " " + RuntimeInformation.OSArchitecture.ToString() + " " + RuntimeInformation.ProcessArchitecture.ToString() + " - " + Environment.MachineName.ToString();

        var infoTable = new Table().Border(TableBorder.Rounded).BorderColor(Theme.GrayColor);
        infoTable.AddColumn(new TableColumn($"[bold]{S["Property"]}[/]").NoWrap().PadRight(2));
        infoTable.AddColumn($"[bold]{S["Value"]}[/]");
        infoTable.AddRow(S["OS"], $"[{Theme.Cyan}]{osLabel}[/]");
        infoTable.AddRow(S["Architecture"], $"[{Theme.Cyan}]{arch}[/]");
        infoTable.AddRow(S["PlatformToolsUrl"], $"[{Theme.Dim}]{Markup.Escape(platform.PlatformToolsUrl)}[/]");
        if (platform.UsbDriverUrl is not null)
            infoTable.AddRow(S["UsbDriverUrl"], $"[{Theme.Dim}]{Markup.Escape(platform.UsbDriverUrl)}[/]");
        AnsiConsole.Write(infoTable);
        AnsiConsole.WriteLine();

        // ── Step 2 — Select components ──────────────────────────
        WriteStep(2, S["StepSelectComponents"]);

        var choices = new List<string> { S["ComponentPlatformTools"], S["ComponentAddToPath"] };
        if (platform.Platform == PlatformType.Windows && platform.UsbDriverUrl is not null)
            choices.Add(S["ComponentUsbDrivers"]);
        choices.Add(S["ComponentVerify"]);

        var selected = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title($"[bold]{S["WhatToInstall"]}[/]")
                .Required()
                .PageSize(10)
                .InstructionsText($"[grey]{S["MultiSelectInstructions"]}[/]")
                .AddChoiceGroup(S["AllComponents"], choices)
                .Select(choices[0])
                .Select(choices[1])
                .Select(choices.Count > 3 ? choices[2] : choices[^1])
                .Select(choices[^1]));

        var installPlatformTools = selected.Contains(S["ComponentPlatformTools"]);
        var addToPath = selected.Contains(S["ComponentAddToPath"]);
        var installDrivers = selected.Contains(S["ComponentUsbDrivers"]);
        var verify = selected.Contains(S["ComponentVerify"]);

        if (!installPlatformTools)
        {
            AnsiConsole.MarkupLine($"[{Theme.Amber}]{S["PlatformToolsRequired"]}[/]");
            return 0;
        }

        AnsiConsole.WriteLine();

        // ── Step 3 — Install level (user/system) ────────────────
        WriteStep(3, S["StepInstallLevel"]);

        var levelChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold]{S["ChooseInstallLevel"]}[/]")
                .AddChoices(S["InstallLevelUser"], S["InstallLevelSystem"]));

        var installLevel = levelChoice == S["InstallLevelSystem"]
            ? InstallLevel.System
            : InstallLevel.User;

        if (installLevel == InstallLevel.System)
            AnsiConsole.MarkupLine($"[{Theme.Amber}]{S["AdminRequired"]}[/]");

        AnsiConsole.WriteLine();

        // ── Step 4 — Install path ───────────────────────────────
        WriteStep(4, S["StepInstallPath"]);

        var defaultPath = platformDetector.GetDefaultInstallPath(installLevel);
        AnsiConsole.MarkupLine($"[{Theme.Dim}]{S["DefaultInstallPath"]}:[/] [{Theme.Cyan}]{Markup.Escape(defaultPath)}[/]");

        var useDefault = AnsiConsole.Confirm(
            S.Format("ConfirmInstallPath", $"[{Theme.Cyan}]{Markup.Escape(defaultPath)}[/]"), defaultValue: true);

        var installPath = useDefault
            ? defaultPath
            : AnsiConsole.Prompt(
                new TextPrompt<string>($"{S["EnterCustomPath"]}")
                    .Validate(p =>
                    {
                        var parent = Path.GetDirectoryName(p);
                        if (string.IsNullOrWhiteSpace(parent))
                            return ValidationResult.Error($"[{Theme.Red}]{S["ParentDirNotExist"]}[/]");
                        if (p.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                            return ValidationResult.Error($"[{Theme.Red}]{S["InvalidPathChars"]}[/]");
                        return Directory.Exists(parent) || Directory.Exists(Path.GetPathRoot(p))
                            ? ValidationResult.Success()
                            : ValidationResult.Error($"[{Theme.Red}]{S["ParentDirNotExist"]}[/]");
                    }));

        AnsiConsole.WriteLine();

        // ── Pre-flight: disk space check ────────────────────────
        try
        {
            var pathRoot = Path.GetPathRoot(installPath);
            if (pathRoot is not null)
            {
                var drive = new DriveInfo(pathRoot);
                if (drive.IsReady && drive.AvailableFreeSpace < 100 * 1024 * 1024)
                {
                    AnsiConsole.MarkupLine($"  [{Theme.Red}]{S["InsufficientDiskSpace"]}[/]");
                    return 1;
                }
            }
        }
        catch { /* DriveInfo may fail on network paths — proceed anyway */ }

        // ── Step 5 — Summary ────────────────────────────────────
        WriteStep(5, S["StepSummary"]);

        var summary = new Table().Border(TableBorder.Rounded).BorderColor(Theme.GrayColor);
        summary.AddColumn(new TableColumn($"[bold]{S["Component"]}[/]").PadRight(2));
        summary.AddColumn(new TableColumn($"[bold]{S["Action"]}[/]"));
        summary.AddRow(S["ComponentPlatformTools"], $"[{Theme.Green}]{S["Install"]}[/]");
        summary.AddRow(S["InstallPath"], $"[{Theme.Cyan}]{Markup.Escape(installPath)}[/]");
        summary.AddRow(S["Level"], installLevel == InstallLevel.System
            ? $"[{Theme.Amber}]{S["SystemLevel"]}[/]"
            : $"[{Theme.Green}]{S["UserLevel"]}[/]");
        summary.AddRow("PATH", addToPath ? $"[{Theme.Green}]{S["Yes"]}[/]" : $"[{Theme.Gray}]{S["Skip"]}[/]");
        summary.AddRow(S["ComponentUsbDrivers"], installDrivers ? $"[{Theme.Green}]{S["Install"]}[/]" : $"[{Theme.Gray}]{S["Skip"]}[/]");
        summary.AddRow(S["ComponentVerify"], verify ? $"[{Theme.Green}]{S["Yes"]}[/]" : $"[{Theme.Gray}]{S["Skip"]}[/]");
        AnsiConsole.Write(summary);
        AnsiConsole.WriteLine();

        if (!AnsiConsole.Confirm($"[bold]{S["StartInstallation"]}[/]"))
        {
            AnsiConsole.MarkupLine($"[{Theme.Amber}]{S["InstallCancelled"]}[/]");
            return 0;
        }

        AnsiConsole.WriteLine();

        // ── Step 6 — Execute installation ───────────────────────
        WriteStep(6, S["StepInstalling"]);

        var options = new InstallOptions
        {
            InstallPath = installPath,
            AddToPath = addToPath,
            InstallUsbDrivers = installDrivers,
            VerifyAfterInstall = verify,
            Level = installLevel
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
                var dlPlatformTask = ctx.AddTask($"[{Theme.Green}]{S["DownloadingPlatformTools"]}[/]", maxValue: 100);
                var exPlatformTask = ctx.AddTask($"[{Theme.Blue}]{S["ExtractingFiles"]}[/]", maxValue: 100);
                exPlatformTask.IsIndeterminate = true;

                ProgressTask? dlUsbTask = null;
                ProgressTask? exUsbTask = null;
                ProgressTask? drvInstallTask = null;

                if (installDrivers)
                {
                    dlUsbTask = ctx.AddTask($"[{Theme.Green}]{S["DownloadingUsbDrivers"]}[/]", maxValue: 100, autoStart: false);
                    exUsbTask = ctx.AddTask($"[{Theme.Blue}]{S["ExtractingUsbDrivers"]}[/]", maxValue: 100, autoStart: false);
                    exUsbTask.IsIndeterminate = true;
                    drvInstallTask = ctx.AddTask($"[{Theme.Amber}]{S["InstallingUsbDrivers"]}[/]", autoStart: false);
                    drvInstallTask.IsIndeterminate = true;
                }

                ProgressTask? pathTask = null;
                if (addToPath)
                {
                    pathTask = ctx.AddTask($"[{Theme.Cyan}]{S["ConfiguringPath"]}[/]", autoStart: false, maxValue: 100);
                    pathTask.IsIndeterminate = true;
                }

                ProgressTask? verTask = null;
                if (verify)
                {
                    verTask = ctx.AddTask($"[{Theme.Blue}]{S["VerifyingInstallation"]}[/]", autoStart: false, maxValue: 100);
                    verTask.IsIndeterminate = true;
                }

                // Track which phase we're in so progress callbacks go to the right task
                var phase = InstallPhase.DownloadPlatformTools;

                orchestrator.DownloadProgress = new Progress<double>(v =>
                {
                    var target = phase == InstallPhase.DownloadUsbDrivers ? dlUsbTask : dlPlatformTask;
                    if (target is null) return;
                    target.Value = Math.Min(v, 100);
                    if (v >= 100 && !target.IsFinished) target.StopTask();
                });

                orchestrator.ExtractProgress = new Progress<double>(v =>
                {
                    var target = phase == InstallPhase.ExtractUsbDrivers ? exUsbTask : exPlatformTask;
                    if (target is null) return;
                    target.IsIndeterminate = false;
                    target.Value = Math.Min(v, 100);
                    if (v >= 100 && !target.IsFinished) target.StopTask();
                });

                orchestrator.OnStatusUpdate = msg =>
                {
                    if (msg.Contains("Downloading USB", StringComparison.OrdinalIgnoreCase))
                    {
                        phase = InstallPhase.DownloadUsbDrivers;
                        dlUsbTask?.StartTask();
                    }
                    else if (msg.Contains("Extracting USB", StringComparison.OrdinalIgnoreCase))
                    {
                        phase = InstallPhase.ExtractUsbDrivers;
                        exUsbTask?.StartTask();
                    }
                    else if (msg.Contains("Installing USB", StringComparison.OrdinalIgnoreCase))
                    {
                        drvInstallTask?.StartTask();
                    }
                    else if (msg.Contains("Configuring PATH", StringComparison.OrdinalIgnoreCase))
                    {
                        pathTask?.StartTask();
                    }
                    else if (msg.Contains("Verifying", StringComparison.OrdinalIgnoreCase))
                    {
                        verTask?.StartTask();
                    }
                };

                var installResult = await orchestrator.InstallAsync(options);

                // Finish any remaining indeterminate tasks on completion
                FinishTask(drvInstallTask);
                FinishTask(pathTask);
                FinishTask(verTask);

                return installResult;
            });

        AnsiConsole.WriteLine();

        // ── Step 7 — Results ────────────────────────────────────
        if (result.Success)
        {
            WriteStep(7, S["StepComplete"]);
            RenderSuccessPanel(result);

            AnsiConsole.WriteLine();
            RenderNextSteps(result);

            AnsiConsole.WriteLine();
            WriteStep(8, S["StepPostInstall"]);

            var postAction = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[bold]{S["WhatToDoNow"]}[/]")
                    .PageSize(5)
                    .AddChoices(S["OpenNewTerminal"], S["RestartComputer"], S["NothingExit"]));

            return HandlePostAction(postAction);
        }

        RenderError(S["InstallFailed"], result.ErrorMessage ?? S["UnknownError"]);
        return 1;
    }

    // ── Helpers ──────────────────────────────────────────────────

    private enum InstallPhase { DownloadPlatformTools, DownloadUsbDrivers, ExtractUsbDrivers }

    private static void FinishTask(ProgressTask? task)
    {
        if (task is not null && !task.IsFinished)
        {
            task.IsIndeterminate = false;
            task.Value = task.MaxValue;
            task.StopTask();
        }
    }

    private static void RenderHeader()
    {
        AnsiConsole.Write(new Rule($"[bold {Theme.Blue}]ADB/USB Latest Driver Installer[/]").LeftJustified().RuleStyle(Theme.Blue));
        AnsiConsole.MarkupLine($"  [{Theme.Dim}]{S["AppSubtitle"]}[/]");
        AnsiConsole.MarkupLine($"  [{Theme.Dim}]{S.Format("LanguageDetected", S.LanguageName)}[/]");
        AnsiConsole.WriteLine();
    }

    private static void WriteStep(int step, string title)
    {
        AnsiConsole.Write(new Rule($"[bold {Theme.Blue}][[{step}]][/] [white]{title}[/]").LeftJustified().RuleStyle(Theme.Gray));
        AnsiConsole.WriteLine();
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

    private static string GetNetworkErrorDetail(HttpRequestException ex)
    {
        if (ex.StatusCode.HasValue)
            return $"HTTP {(int)ex.StatusCode.Value} — {ex.Message}";
        if (ex.InnerException is SocketException socketEx)
            return $"{S["NetworkError"]}: {socketEx.Message}";
        return ex.Message;
    }

    private static void RenderSuccessPanel(InstallResult result)
    {
        var grid = new Grid();
        grid.AddColumn(new GridColumn().PadRight(2));
        grid.AddColumn();

        grid.AddRow($"[{Theme.Dim}]{S["Path"]}:[/]", $"[{Theme.Cyan}]{Markup.Escape(result.PlatformToolsPath ?? Theme.Dash)}[/]");
        if (result.AdbVersion is not null)
            grid.AddRow($"[{Theme.Dim}]ADB:[/]", $"[{Theme.Green}]{Markup.Escape(result.AdbVersion)}[/]");
        if (result.FastbootVersion is not null)
            grid.AddRow($"[{Theme.Dim}]Fastboot:[/]", $"[{Theme.Green}]{Markup.Escape(result.FastbootVersion)}[/]");
        grid.AddRow($"[{Theme.Dim}]PATH:[/]", result.PathConfigured
            ? $"[{Theme.Green}]{Theme.Ok} {S["AddedToPath"]}[/]"
            : $"[{Theme.Gray}]{Theme.Dash} {S["PathNotModified"]}[/]");
        grid.AddRow($"[{Theme.Dim}]{S["ComponentUsbDrivers"]}:[/]", result.UsbDriversInstalled
            ? $"[{Theme.Green}]{Theme.Ok} {S["UsbDriversInstalled"]}[/]"
            : $"[{Theme.Gray}]{Theme.Dash} {S["UsbDriversSkipped"]}[/]");

        AnsiConsole.Write(new Panel(grid)
            .Header($"[{Theme.Green} bold] {Theme.Ok} {S["InstallSuccessful"]} [/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Theme.GreenColor)
            .Padding(1, 0)
            .Expand());
    }

    private static void RenderNextSteps(InstallResult result)
    {
        var tips = new Grid();
        tips.AddColumn(new GridColumn().PadRight(1));
        tips.AddColumn();

        tips.AddRow($"[{Theme.Amber}]1.[/]", result.PathConfigured
            ? $"[white]{S["NextStepOpenTerminal"]}[/]"
            : $"[white]{S["NextStepAddPathManual"]}[/]");
        tips.AddRow($"[{Theme.Amber}]2.[/]", $"[white]{S["NextStepUsbDebugging"]}[/]");
        tips.AddRow($"[{Theme.Amber}]3.[/]", $"[white]{S["NextStepAdbDevices"]}[/]");
        tips.AddRow($"[{Theme.Amber}]4.[/]", $"[white]{S["NextStepVerify"]}[/]");
        tips.AddRow($"[{Theme.Amber}]5.[/]", $"[white]{S["NextStepUpdate"]}[/]");

        AnsiConsole.Write(new Panel(tips)
            .Header($"[{Theme.Blue} bold] {S["NextStepsHeader"]} [/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Theme.BlueColor)
            .Padding(1, 0)
            .Expand());
    }

    private static int HandlePostAction(string action)
    {
        if (action == S["OpenNewTerminal"])
        {
            AnsiConsole.MarkupLine($"[{Theme.Dim}]{S["OpeningTerminal"]}[/]");
            try
            {
                if (OperatingSystem.IsWindows())
                    Process.Start(new ProcessStartInfo("cmd", "/k echo ADB is ready! Try: adb version") { UseShellExecute = true });
                else if (OperatingSystem.IsMacOS())
                    Process.Start(new ProcessStartInfo("open", "-a Terminal") { UseShellExecute = true });
                else
                    Process.Start(new ProcessStartInfo("x-terminal-emulator") { UseShellExecute = true });
            }
            catch
            {
                AnsiConsole.MarkupLine($"[{Theme.Amber}]{S["CouldNotOpenTerminal"]}[/]");
            }

            return 0;
        }

        if (action == S["RestartComputer"])
        {
            AnsiConsole.MarkupLine($"[bold {Theme.Red}]{S["SystemRestartIn"]}[/]");
            AnsiConsole.MarkupLine($"[{Theme.Dim}]{S["PressCancelRestart"]}[/]");

            try
            {
                if (OperatingSystem.IsWindows())
                    Process.Start(new ProcessStartInfo("shutdown", "/r /t 10") { UseShellExecute = true });
                else
                    Process.Start(new ProcessStartInfo("shutdown", "-r +0") { UseShellExecute = true });
            }
            catch
            {
                AnsiConsole.MarkupLine($"[{Theme.Amber}]{S["CouldNotRestart"]}[/]");
            }

            return 0;
        }

        AnsiConsole.MarkupLine($"[{Theme.Green}]{S["DoneOpenTerminal"]}[/]");
        return 0;
    }
}
