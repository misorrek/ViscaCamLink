namespace ViscaCamLink.ViewModels;

public class PresetItemViewModel(int slotIndex, string name) : ViewModelBase
{
    private string _name = name;

    public int SlotIndex { get; } = slotIndex;

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
}
