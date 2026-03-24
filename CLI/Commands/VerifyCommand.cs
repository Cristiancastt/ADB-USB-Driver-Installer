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
            AnsiConsole.MarkupLine($"[red]{S["UnexpectedError"]}:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    private async Task<int> RunVerifyAsync()
    {
        AnsiConsole.Write(new FigletText("ADB Verify").Color(Color.Blue));
        AnsiConsole.Write(new Rule($"[bold blue]{S["VerificationTitle"]}[/]").LeftJustified());
        AnsiConsole.WriteLine();

        var defaultPath = platformDetector.GetDefaultInstallPath();

        var result = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots2)
            .SpinnerStyle(Style.Parse("green bold"))
            .StartAsync(S["ScanningAdb"],
                async _ => await adbVerifier.VerifyAsync(defaultPath));

        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Blue).Expand();
        table.AddColumn(new TableColumn($"[bold]{S["Component"]}[/]").Centered());
        table.AddColumn(new TableColumn($"[bold]{S["Status"]}[/]").Centered());
        table.AddColumn(new TableColumn($"[bold]{S["Details"]}[/]"));

        table.AddRow(
            "ADB",
            result.AdbFound ? $"[green bold]{S["Found"]}[/]" : $"[red bold]{S["NotFound"]}[/]",
            result.AdbVersion ?? result.AdbPath ?? "[grey]—[/]");

        table.AddRow(
            "Fastboot",
            result.FastbootFound ? $"[green bold]{S["Found"]}[/]" : $"[red bold]{S["NotFound"]}[/]",
            result.FastbootVersion ?? result.FastbootPath ?? "[grey]—[/]");

        table.AddRow(
            S["InPath"],
            result.IsInPath ? $"[green bold]{S["Yes"]}[/]" : $"[yellow bold]{S["No"]}[/]",
            result.IsInPath ? $"[dim]{S["AccessibleGlobally"]}[/]" : $"[dim]{S["OnlyInInstallDir"]}[/]");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        if (result is { AdbFound: true, FastbootFound: true })
        {
            AnsiConsole.Write(new Panel($"[green bold]{S["EverythingGood"]}[/]")
                .Border(BoxBorder.Double).BorderColor(Color.Green).Expand());
            return 0;
        }

        AnsiConsole.Write(new Panel($"[yellow]{S["ComponentsMissing"]}[/]")
            .Border(BoxBorder.Rounded).BorderColor(Color.Yellow).Expand());
        return 1;
    }
}
