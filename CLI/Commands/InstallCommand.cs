using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using AdbDriverInstaller.CLI.Localization;
using AdbDriverInstaller.Core.Enums;
using AdbDriverInstaller.Core.Interfaces;
using AdbDriverInstaller.Core.Models;
using AdbDriverInstaller.Infrastructure.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AdbDriverInstaller.CLI.Commands;

public sealed class InstallCommand(
    IPlatformDetector platformDetector,
    InstallOrchestrator orchestrator) : AsyncCommand
{
    private static Localizer S => Localizer.Instance;

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        try
        {
            return await RunWizardAsync();
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[yellow]{S["InstallCancelled"]}[/]");
            return 1;
        }
        catch (HttpRequestException ex)
        {
            RenderError(S["NetworkError"], GetNetworkErrorDetail(ex));
            return 1;
        }
        catch (SocketException ex)
        {
            RenderError(S["NetworkError"], ex.Message);
            return 1;
        }
        catch (UnauthorizedAccessException ex)
        {
            RenderError(S["PermissionError"], ex.Message);
            return 1;
        }
        catch (IOException ex)
        {
            RenderError(S["FileSystemError"], ex.Message);
            return 1;
        }
        catch (Exception ex)
        {
            RenderError(S["UnexpectedError"], ex.Message);
            return 1;
        }
    }

    private async Task<int> RunWizardAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("ADB Installer").Color(Color.Green));
        AnsiConsole.Write(new Rule($"[bold grey]{S["AppSubtitle"]}[/]").LeftJustified());
        AnsiConsole.MarkupLine($"[dim]{S.Format("LanguageDetected", S.LanguageName)}[/]");
        AnsiConsole.WriteLine();

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
        var arch = RuntimeInformation.OSArchitecture.ToString();

        var infoTable = new Table().Border(TableBorder.Rounded).BorderColor(Color.Teal);
        infoTable.AddColumn(new TableColumn($"[bold]{S["Property"]}[/]").NoWrap());
        infoTable.AddColumn($"[bold]{S["Value"]}[/]");
        infoTable.AddRow(S["OS"], $"[cyan]{osLabel}[/]");
        infoTable.AddRow(S["Architecture"], $"[cyan]{arch}[/]");
        infoTable.AddRow(S["PlatformToolsUrl"], $"[dim]{Markup.Escape(platform.PlatformToolsUrl)}[/]");
        if (platform.UsbDriverUrl is not null)
            infoTable.AddRow(S["UsbDriverUrl"], $"[dim]{Markup.Escape(platform.UsbDriverUrl)}[/]");
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
            AnsiConsole.MarkupLine($"[yellow]{S["PlatformToolsRequired"]}[/]");
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
            AnsiConsole.MarkupLine($"[yellow]{S["AdminRequired"]}[/]");

        AnsiConsole.WriteLine();

        // ── Step 4 — Install path ───────────────────────────────
        WriteStep(4, S["StepInstallPath"]);

        var defaultPath = platformDetector.GetDefaultInstallPath(installLevel);
        AnsiConsole.MarkupLine($"[dim]{S["DefaultInstallPath"]}:[/] [cyan]{Markup.Escape(defaultPath)}[/]");

        var useDefault = AnsiConsole.Confirm(
            S.Format("ConfirmInstallPath", $"[cyan]{Markup.Escape(defaultPath)}[/]"), defaultValue: true);

        var installPath = useDefault
            ? defaultPath
            : AnsiConsole.Prompt(
                new TextPrompt<string>($"{S["EnterCustomPath"]}")
                    .Validate(p =>
                    {
                        var parent = Path.GetDirectoryName(p);
                        if (string.IsNullOrWhiteSpace(parent))
                            return ValidationResult.Error($"[red]{S["ParentDirNotExist"]}[/]");
                        return Directory.Exists(parent) || Directory.Exists(Path.GetPathRoot(p))
                            ? ValidationResult.Success()
                            : ValidationResult.Error($"[red]{S["ParentDirNotExist"]}[/]");
                    }));

        AnsiConsole.WriteLine();

        // ── Step 5 — Summary ────────────────────────────────────
        WriteStep(5, S["StepSummary"]);

        var summary = new Table().Border(TableBorder.Heavy).BorderColor(Color.Green);
        summary.AddColumn($"[bold]{S["Component"]}[/]");
        summary.AddColumn($"[bold]{S["Action"]}[/]");
        summary.AddRow("Platform Tools", $"[green]{S["Install"]}[/]");
        summary.AddRow(S["InstallPath"], $"[cyan]{Markup.Escape(installPath)}[/]");
        summary.AddRow(S["Level"], installLevel == InstallLevel.System
            ? $"[yellow]{S["SystemLevel"]}[/]"
            : $"[green]{S["UserLevel"]}[/]");
        summary.AddRow("PATH", addToPath ? $"[green]{S["Yes"]}[/]" : $"[grey]{S["Skip"]}[/]");
        summary.AddRow("USB Drivers", installDrivers ? $"[green]{S["Install"]}[/]" : $"[grey]{S["Skip"]}[/]");
        summary.AddRow(S["ComponentVerify"], verify ? $"[green]{S["Yes"]}[/]" : $"[grey]{S["Skip"]}[/]");
        AnsiConsole.Write(summary);
        AnsiConsole.WriteLine();

        if (!AnsiConsole.Confirm($"[bold]{S["StartInstallation"]}[/]"))
        {
            AnsiConsole.MarkupLine($"[yellow]{S["InstallCancelled"]}[/]");
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
                new PercentageColumn(),
                new RemainingTimeColumn(),
                new TransferSpeedColumn())
            .StartAsync(async ctx =>
            {
                // Create all possible progress tasks up-front so they can be
                // safely referenced from callbacks without restarting stopped tasks.
                var dlPlatformTask = ctx.AddTask($"[green]{S["DownloadingPlatformTools"]}[/]", maxValue: 100);
                var exPlatformTask = ctx.AddTask($"[blue]{S["ExtractingFiles"]}[/]", maxValue: 100);
                exPlatformTask.IsIndeterminate = true;

                ProgressTask? dlUsbTask = null;
                ProgressTask? exUsbTask = null;
                ProgressTask? drvInstallTask = null;

                if (installDrivers)
                {
                    dlUsbTask = ctx.AddTask($"[green]{S["DownloadingUsbDrivers"]}[/]", maxValue: 100, autoStart: false);
                    exUsbTask = ctx.AddTask($"[blue]{S["ExtractingUsbDrivers"]}[/]", maxValue: 100, autoStart: false);
                    exUsbTask.IsIndeterminate = true;
                    drvInstallTask = ctx.AddTask($"[yellow]{S["InstallingUsbDrivers"]}[/]", autoStart: false);
                    drvInstallTask.IsIndeterminate = true;
                }

                ProgressTask? pathTask = null;
                if (addToPath)
                {
                    pathTask = ctx.AddTask($"[magenta]{S["ConfiguringPath"]}[/]", autoStart: false, maxValue: 100);
                    pathTask.IsIndeterminate = true;
                }

                ProgressTask? verTask = null;
                if (verify)
                {
                    verTask = ctx.AddTask($"[cyan]{S["VerifyingInstallation"]}[/]", autoStart: false, maxValue: 100);
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
                StopIfRunning(drvInstallTask);
                StopIfRunning(pathTask);
                StopIfRunning(verTask);

                return installResult;
            });

        AnsiConsole.WriteLine();

        // ── Step 7 — Results ────────────────────────────────────
        if (result.Success)
        {
            WriteStep(7, S["StepComplete"]);
            RenderSuccessPanel(result);

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

    private static void StopIfRunning(ProgressTask? task)
    {
        if (task is not null && !task.IsFinished)
        {
            task.IsIndeterminate = false;
            task.Value = task.MaxValue;
            task.StopTask();
        }
    }

    private static void WriteStep(int step, string title)
    {
        AnsiConsole.Write(new Rule($"[bold yellow]{S.Format("Step", step)}[/] — [bold]{title}[/]").LeftJustified());
        AnsiConsole.WriteLine();
    }

    private static void RenderError(string title, string detail)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Panel($"[red]{Markup.Escape(detail)}[/]")
            .Header($"[red bold] {Markup.Escape(title)} [/]")
            .Border(BoxBorder.Heavy)
            .BorderColor(Color.Red)
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
        var tree = new Tree($"[green bold]{S["InstallSuccessful"]}[/]");

        var detailsNode = tree.AddNode($"[bold]{S["Details"]}[/]");
        detailsNode.AddNode($"[dim]{S["Path"]}:[/]  [cyan]{Markup.Escape(result.PlatformToolsPath ?? "—")}[/]");

        if (result.AdbVersion is not null)
            detailsNode.AddNode($"[dim]ADB:[/]   [green]{Markup.Escape(result.AdbVersion)}[/]");
        if (result.FastbootVersion is not null)
            detailsNode.AddNode($"[dim]Fastboot:[/] [green]{Markup.Escape(result.FastbootVersion)}[/]");

        var statusNode = tree.AddNode($"[bold]{S["Status"]}[/]");
        statusNode.AddNode(result.PathConfigured
            ? $"[green]{S["AddedToPath"]}[/]"
            : $"[grey]{S["PathNotModified"]}[/]");
        statusNode.AddNode(result.UsbDriversInstalled
            ? $"[green]{S["UsbDriversInstalled"]}[/]"
            : $"[grey]{S["UsbDriversSkipped"]}[/]");

        AnsiConsole.Write(new Panel(tree)
            .Border(BoxBorder.Double)
            .BorderColor(Color.Green)
            .Expand());
    }

    private static int HandlePostAction(string action)
    {
        if (action == S["OpenNewTerminal"])
        {
            AnsiConsole.MarkupLine($"[dim]{S["OpeningTerminal"]}[/]");
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
                AnsiConsole.MarkupLine($"[yellow]{S["CouldNotOpenTerminal"]}[/]");
            }

            return 0;
        }

        if (action == S["RestartComputer"])
        {
            AnsiConsole.MarkupLine($"[bold red]{S["SystemRestartIn"]}[/]");
            AnsiConsole.MarkupLine($"[dim]{S["PressCancelRestart"]}[/]");

            try
            {
                if (OperatingSystem.IsWindows())
                    Process.Start(new ProcessStartInfo("shutdown", "/r /t 10") { UseShellExecute = true });
                else
                    Process.Start(new ProcessStartInfo("shutdown", "-r +0") { UseShellExecute = true });
            }
            catch
            {
                AnsiConsole.MarkupLine($"[yellow]{S["CouldNotRestart"]}[/]");
            }

            return 0;
        }

        AnsiConsole.MarkupLine($"[green]{S["DoneOpenTerminal"]}[/]");
        return 0;
    }
}
