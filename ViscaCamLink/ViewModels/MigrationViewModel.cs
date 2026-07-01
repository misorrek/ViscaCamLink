namespace ViscaCamLink.ViewModels;

using System.Diagnostics;
using System.Windows.Input;

using ViscaCamLink.Util;

public sealed class MigrationViewModel : ViewModelBase
{
    public MigrationViewModel(string uninstallString, Action closeAction)
    {
        UninstallCommand = new Command(() =>
        {
            LaunchUninstaller(uninstallString);
            closeAction();
        });

        CloseCommand = new Command(closeAction);
    }

    public ICommand UninstallCommand { get; }

    public ICommand CloseCommand { get; }

    private static void LaunchUninstaller(string uninstallString)
    {
        var (exe, args) = ParseUninstallString(uninstallString);
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                UseShellExecute = true,
            });
        }
        catch
        {
            // Best-effort: if launch fails the user can still remove the old
            // version via Windows Settings → Apps.
        }
    }

    private static (string exe, string args) ParseUninstallString(string uninstallString)
    {
        uninstallString = uninstallString.Trim();

        if (uninstallString.StartsWith('"'))
        {
            var closingQuote = uninstallString.IndexOf('"', 1);
            if (closingQuote >= 0)
            {
                var exe = uninstallString[1..closingQuote];
                var args = uninstallString[(closingQuote + 1)..].Trim();
                return (exe, args);
            }
        }

        var spaceIndex = uninstallString.IndexOf(' ');
        return spaceIndex >= 0
            ? (uninstallString[..spaceIndex], uninstallString[(spaceIndex + 1)..])
            : (uninstallString, string.Empty);
    }
}
