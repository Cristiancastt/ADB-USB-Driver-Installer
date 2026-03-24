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
        catch (Exception ex)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Panel(new Markup($"[red]{Markup.Escape(ex.Message)}[/]"))
                .Header($"[red bold] {S["UnexpectedError"]} [/]")
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Red)
                .Padding(1, 0)
                .Expand());
            var logPath = CrashLogger.WriteLog(ex, "verify");
            AnsiConsole.MarkupLine($"  [dim]Log: {Markup.Escape(logPath)}[/]");
            return 1;
        }
    }

    private async Task<int> RunVerifyAsync()
    {
        AnsiConsole.Write(new Rule($"[bold dodgerblue1]ADB Verify[/]").LeftJustified().RuleStyle("grey"));
        AnsiConsole.MarkupLine($"  [dim]{S["VerificationTitle"]}[/]");
        AnsiConsole.WriteLine();

        var defaultPath = platformDetector.GetDefaultInstallPath();

        var result = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots2)
            .SpinnerStyle(Style.Parse("dodgerblue1"))
            .StartAsync(S["ScanningAdb"],
                async _ => await adbVerifier.VerifyAsync(defaultPath));

        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey).Expand();
        table.AddColumn(new TableColumn($"[bold]{S["Component"]}[/]").Centered());
        table.AddColumn(new TableColumn($"[bold]{S["Status"]}[/]").Centered());
        table.AddColumn(new TableColumn($"[bold]{S["Details"]}[/]"));

        table.AddRow(
            "ADB",
            result.AdbFound ? $"[green]{S["Found"]}[/]" : $"[red]{S["NotFound"]}[/]",
            result.AdbVersion ?? result.AdbPath ?? "[grey]—[/]");

        table.AddRow(
            "Fastboot",
            result.FastbootFound ? $"[green]{S["Found"]}[/]" : $"[red]{S["NotFound"]}[/]",
            result.FastbootVersion ?? result.FastbootPath ?? "[grey]—[/]");

        table.AddRow(
            S["InPath"],
            result.IsInPath ? $"[green]{S["Yes"]}[/]" : $"[yellow]{S["No"]}[/]",
            result.IsInPath ? $"[dim]{S["AccessibleGlobally"]}[/]" : $"[dim]{S["OnlyInInstallDir"]}[/]");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        if (result is { AdbFound: true, FastbootFound: true })
        {
            AnsiConsole.MarkupLine($"  [green]{S["EverythingGood"]}[/]");
            return 0;
        }

        AnsiConsole.MarkupLine($"  [yellow]{S["ComponentsMissing"]}[/]");
        return 1;
    }
}
