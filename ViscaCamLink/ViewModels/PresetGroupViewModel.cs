namespace ViscaCamLink.ViewModels;

using System.ComponentModel;
using System.Runtime.CompilerServices;

public sealed class PresetGroupViewModel : INotifyPropertyChanged
{
    private string _name;
    private bool _isActive;
    private bool _isRenaming;

    public PresetGroupViewModel(string id, string name, bool isActive)
    {
        Id = id;
        _name = name;
        _isActive = isActive;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
            {
                return;
            }

            _name = value;
            OnPropertyChanged();
        }
    }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value)
            {
                return;
            }

            _isActive = value;
            OnPropertyChanged();
        }
    }

    public bool IsRenaming
    {
        get => _isRenaming;
        set
        {
            if (_isRenaming == value)
            {
                return;
            }

            _isRenaming = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
