using AdbDriverInstaller.CLI.Infrastructure;
using AdbDriverInstaller.CLI.Localization;
using AdbDriverInstaller.Core.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AdbDriverInstaller.CLI.Commands;

public sealed class VerifyCommand(IAdbVerifier adbVerifier, IPlatformDetector platformDetector)
    : AsyncCommand
{
    private static Localizer S => Localizer.Instance;

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        try
        {
            return await RunVerifyAsync();
        }
        catch (TaskCanceledException)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"  [{Theme.Amber}]{S["OperationTimedOut"]}[/]");
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
            var logPath = CrashLogger.WriteLog(ex, "verify");
            AnsiConsole.MarkupLine($"  [{Theme.Dim}]Log: {Markup.Escape(logPath)}[/]");
            return 1;
        }
    }

    private async Task<int> RunVerifyAsync()
    {
        AnsiConsole.Write(new Rule($"[bold {Theme.Blue}]{S["VerifyHeader"]}[/]").LeftJustified().RuleStyle(Theme.Gray));
        AnsiConsole.MarkupLine($"  [{Theme.Dim}]{S["VerificationTitle"]}[/]");
        AnsiConsole.WriteLine();

        var defaultPath = platformDetector.GetDefaultInstallPath();

        var result = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots2)
            .SpinnerStyle(Style.Parse(Theme.Blue))
            .StartAsync(S["ScanningAdb"],
                async _ => await adbVerifier.VerifyAsync(defaultPath));

        var table = new Table().Border(TableBorder.Rounded).BorderColor(Theme.GrayColor).Expand();
        table.AddColumn(new TableColumn($"[bold]{S["Component"]}[/]").Centered());
        table.AddColumn(new TableColumn($"[bold]{S["Status"]}[/]").Centered());
        table.AddColumn(new TableColumn($"[bold]{S["Details"]}[/]"));

        table.AddRow(
            "ADB",
            result.AdbFound ? $"[{Theme.Green}]{Theme.Ok} {S["Found"]}[/]" : $"[{Theme.Red}]{Theme.Fail} {S["NotFound"]}[/]",
            result.AdbVersion ?? result.AdbPath ?? $"[{Theme.Gray}]{Theme.Dash}[/]");

        table.AddRow(
            "Fastboot",
            result.FastbootFound ? $"[{Theme.Green}]{Theme.Ok} {S["Found"]}[/]" : $"[{Theme.Red}]{Theme.Fail} {S["NotFound"]}[/]",
            result.FastbootVersion ?? result.FastbootPath ?? $"[{Theme.Gray}]{Theme.Dash}[/]");

        table.AddRow(
            S["InPath"],
            result.IsInPath ? $"[{Theme.Green}]{S["Yes"]}[/]" : $"[{Theme.Amber}]{S["No"]}[/]",
            result.IsInPath ? $"[{Theme.Dim}]{S["AccessibleGlobally"]}[/]" : $"[{Theme.Dim}]{S["OnlyInInstallDir"]}[/]");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        if (result is { AdbFound: true, FastbootFound: true })
        {
            AnsiConsole.MarkupLine($"  [{Theme.Green}]{Theme.Ok} {S["EverythingGood"]}[/]");
            return 0;
        }

        AnsiConsole.MarkupLine($"  [{Theme.Amber}]{S["ComponentsMissing"]}[/]");
        return 1;
    }
}
