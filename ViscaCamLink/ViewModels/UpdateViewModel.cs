namespace ViscaCamLink.ViewModels;

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

using AutoUpdaterDotNET;

using ViscaCamLink.Util;

public class UpdateViewModel : INotifyPropertyChanged
{
    public UpdateViewModel(UpdateInfoEventArgs updateInfoEventArgs, Action closeHandler)
    {
        UpdateInfoEventArgs = updateInfoEventArgs;
        CloseHandler = closeHandler;

        UpdateCommand = new Command(ExecuteUpdate);
        CancelCommand = new Command(ExecuteCancel);            
    }

    public event PropertyChangedEventHandler? PropertyChanged;        

    public ICommand UpdateCommand { get; }

    public ICommand CancelCommand { get; }

    public String VersionText => $"{UpdateInfoEventArgs.CurrentVersion} (Aktuell: {UpdateInfoEventArgs.InstalledVersion})";

    public String ChangelogUrl => UpdateInfoEventArgs.ChangelogURL;

    private UpdateInfoEventArgs UpdateInfoEventArgs { get; }

    private Action CloseHandler { get; }

    protected void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    private void ExecuteUpdate()
    {
        if (AutoUpdater.DownloadUpdate(UpdateInfoEventArgs))
        {
            CloseHandler.Invoke();
            Application.Current.MainWindow.Close();
        }
    }
    private void ExecuteCancel()
    {
        CloseHandler.Invoke();
    }
}
