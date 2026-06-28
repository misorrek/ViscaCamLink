namespace ViscaCamLink.ViewModels;

using System.ComponentModel;
using System.Runtime.CompilerServices;

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
        catch
        {
            // Intentionally swallowed — camera operations are best-effort.
            // Persistent failures are surfaced via ConnectionStatusChanged events.
        }
    }

    protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
