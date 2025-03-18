namespace ViscaCamLink.Common;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

public abstract class BaseViewModel : INotifyPropertyChanged, IBaseViewModel
{
    protected BaseViewModel()
    {
        CloseWindowCommand = new Command(ExecuteCloseWindow, CanCloseWindow);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand CloseWindowCommand { get; }

    public Action? RequestCloseDialog { get; set; }

    protected void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected virtual Boolean CanCloseWindow()
    {
        return true;
    }

    private void ExecuteCloseWindow()
    {
        RequestCloseDialog?.Invoke();
    }
}
