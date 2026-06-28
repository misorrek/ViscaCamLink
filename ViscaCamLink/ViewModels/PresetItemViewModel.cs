namespace ViscaCamLink.ViewModels;

using System.ComponentModel;
using System.Runtime.CompilerServices;

public sealed class PresetItemViewModel : INotifyPropertyChanged
{
    private string _name;

    public PresetItemViewModel(int slotIndex, string name)
    {
        SlotIndex = slotIndex;
        _name = name;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public int SlotIndex { get; }

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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
        }
    }
}
