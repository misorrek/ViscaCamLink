namespace ViscaCamLink.ViewModels;

using System;

using ViscaCamLink.Repositories.AppSettings;

public class CameraProfileItemViewModel(CameraProfile profile) : ViewModelBase
{
    private string _name = profile.Name;
    private string _ip = profile.Ip;
    private int _port = profile.Port;

    public Guid Id { get; } = profile.Id;

    public string Name
    {
        get => _name;
        private set
        {
            _name = value;

            NotifyPropertyChanged();
        }
    }

    public string Ip
    {
        get => _ip;
        private set
        {
            _ip = value;

            NotifyPropertyChanged();
        }
    }

    public int Port
    {
        get => _port;
        private set
        {
            _port = value;

            NotifyPropertyChanged();
        }
    }

    public void Update(CameraProfile profile)
    {
        Name = profile.Name;
        Ip = profile.Ip;
        Port = profile.Port;
    }
}
