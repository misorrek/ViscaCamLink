namespace ViscaCamLink.ViewModels;

using System;
using System.Diagnostics;
using System.Windows.Input;

using ViscaCamLink.Infrastructure.Interface;

public class MigrationViewModel(string uninstallString, Action closeAction) : ViewModelBase
{
    public ICommand UninstallCommand { get; } = new Command(() =>
        {
            LaunchUninstaller(uninstallString);
            closeAction();
        });

    public ICommand CloseCommand { get; } = new Command(closeAction);

    private static void LaunchUninstaller(string uninstallString)
    {
        var (executable, arguments) = ParseUninstallString(uninstallString);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                UseShellExecute = true,
            });
        }
        catch
        {
            // Best-effort: if launch fails the user can still remove the old version manually.
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
}
