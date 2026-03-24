using System.Runtime.InteropServices;

namespace AdbDriverInstaller.CLI.Infrastructure;

public static class CrashLogger
{
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "adb-installer", "logs");

    public static string WriteLog(Exception ex, string command)
    {
        Directory.CreateDirectory(LogDirectory);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var logFile = Path.Combine(LogDirectory, $"crash_{timestamp}.log");

        var content = $"""
            ADB Driver Installer — Crash Report
            =====================================
            Timestamp : {DateTime.Now:yyyy-MM-dd HH:mm:ss}
            Command   : {command}
            OS        : {RuntimeInformation.OSDescription}
            Arch      : {RuntimeInformation.OSArchitecture}
            Runtime   : {RuntimeInformation.FrameworkDescription}

            Exception
            ---------
            Type    : {ex.GetType().FullName}
            Message : {ex.Message}

            Stack Trace
            -----------
            {ex.StackTrace}
            {FormatInnerExceptions(ex)}
            """;

        File.WriteAllText(logFile, content);
        return logFile;
    }

    private static string FormatInnerExceptions(Exception ex)
    {
        if (ex.InnerException is null) return "";

        var inner = ex.InnerException;
        var result = $"""

            Inner Exception
            ---------------
            Type    : {inner.GetType().FullName}
            Message : {inner.Message}

            {inner.StackTrace}
            """;

        if (inner.InnerException is not null)
            result += FormatInnerExceptions(inner);

        return result;
    }
}
