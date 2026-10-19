namespace ViscaCamLink.ViewModels;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

using Microsoft.Extensions.Logging;

using ViscaCamLink.Infrastructure.Interface;

public partial class MigrationViewModel(
    string uninstallString,
    Action closeAction,
    ILogger<MigrationViewModel> logger) : ViewModelBase
{
    public ICommand UninstallCommand { get; } = new Command(() =>
        {
            LaunchUninstaller(uninstallString, logger);
            closeAction();
        });

    public ICommand CloseCommand { get; } = new Command(closeAction);

    private static void LaunchUninstaller(string uninstallString, ILogger logger)
    {
        var (executable, arguments) = ParseUninstallString(uninstallString);

        LogLaunchingUninstaller(logger, executable);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                UseShellExecute = true,
            });
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException or IOException)
        {
            // Best-effort: if launch fails the user can still remove the old version manually.
            LogLaunchUninstallerFailed(logger, executable, exception);
        }
    }

    private static (string Executable, string Arguments) ParseUninstallString(string uninstallString)
    {
        uninstallString = uninstallString.Trim();

        if (uninstallString.StartsWith('"'))
        {
            var closingQuote = uninstallString.IndexOf('"', 1);

            if (closingQuote >= 0)
            {
                var executable = uninstallString[1..closingQuote];
                var arguments = uninstallString[(closingQuote + 1)..].Trim();

                return (executable, arguments);
            }
        }

        var spaceIndex = uninstallString.IndexOf(' ');

        return spaceIndex >= 0
            ? (uninstallString[..spaceIndex], uninstallString[(spaceIndex + 1)..])
            : (uninstallString, string.Empty);
    }

    [LoggerMessage(EventId = 5201, Level = LogLevel.Information, Message = "Launching legacy 0.3.0 uninstaller '{Executable}'")]
    private static partial void LogLaunchingUninstaller(ILogger logger, string executable);

    [LoggerMessage(EventId = 5202, Level = LogLevel.Warning, Message = "Legacy 0.3.0 uninstaller '{Executable}' could not be started")]
    private static partial void LogLaunchUninstallerFailed(ILogger logger, string executable, Exception exception);
}
