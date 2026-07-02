namespace ViscaCamLink.ViewModels;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ViscaCamLink.Repositories.HotKeys;
using ViscaCamLink.Util;

public sealed class HotKeyBindingItemViewModel : INotifyPropertyChanged
{
    private ModifierKeys _modifier;
    private Key _key;
    private bool _isCapturing;
    private bool _hasConflict;

    public HotKeyBindingItemViewModel(HotKeyBinding binding)
    {
        Action = binding.Action;
        _modifier = binding.Modifier;
        _key = binding.Key;

        TranslationSource.Instance.LanguageChanged += OnLanguageChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public HotKeyAction Action { get; }

    public string ActionDisplayName => HotKeyDefinitions.GetDisplayName(Action);

    public ModifierKeys Modifier
    {
        get => _modifier;
        set
        {
            if (_modifier == value) return;
            _modifier = value;
            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(GestureText));
            NotifyPropertyChanged(nameof(ButtonText));
        }
    }

    public Key Key
    {
        get => _key;
        set
        {
            if (_key == value) return;
            _key = value;
            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(GestureText));
            NotifyPropertyChanged(nameof(ButtonText));
        }
    }

    public bool IsCapturing
    {
        get => _isCapturing;
        set
        {
            if (_isCapturing == value) return;
            _isCapturing = value;
            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(ButtonText));
        }
    }

    public bool HasConflict
    {
        get => _hasConflict;
        set
        {
            if (_hasConflict == value) return;
            _hasConflict = value;
            NotifyPropertyChanged();
        }
    }

    public string GestureText => Key == Key.None
        ? TranslationSource.Instance["HotKey_NotSet"]
        : (Modifier == ModifierKeys.None ? Key.ToString() : $"{Modifier}+{Key}");

    public string ButtonText => IsCapturing ? "Press a key..." : GestureText;

    public HotKeyBinding ToBinding() => new()
    {
        Action = Action,
        Modifier = Modifier,
        Key = Key,
    };

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        NotifyPropertyChanged(nameof(ActionDisplayName));
        if (_key == Key.None)
        {
            NotifyPropertyChanged(nameof(GestureText));
            NotifyPropertyChanged(nameof(ButtonText));
        }
    }
}