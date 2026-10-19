namespace ViscaCamLink.ViewModels;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Awaits a camera operation task, swallowing any exceptions.
    /// Camera failures are best-effort; persistent failures surface through connection status events.
    /// </summary>
    protected static async Task TryCameraOperation(Task operation)
    {
        try
        {
            await operation;
        }
        catch (Exception)
        {
            // Intentionally swallowed — camera operations are best-effort.
            // Persistent failures are surfaced via ConnectionStatusChanged events.
            // Not logged here to avoid double logging: ViscaController logs every failed command at Debug,
            // and services log user actions (power, presets) with context at Error before rethrowing.
        }
    }

    protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
