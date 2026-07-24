namespace ViscaCamLink.ViewModels;

using System;

public class PresetGroupViewModel(Guid id, string name, bool isActive) : ViewModelBase
{
    private string _name = name;
    private bool _isActive = isActive;
    private bool _isRenaming;

    public Guid Id { get; } = id;

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

            NotifyPropertyChanged();
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

            NotifyPropertyChanged();
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

            NotifyPropertyChanged();
        }
    }
}
