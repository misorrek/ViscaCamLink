namespace ViscaCamLink.ViewModels;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

using ViscaCamLink.Infrastructure.Interface;
using ViscaCamLink.Infrastructure.Localization;
using ViscaCamLink.Infrastructure.Theming;
using ViscaCamLink.Services;

public class OptionsViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;
    private readonly IHotKeyService _hotKeyService;
    private readonly Action _closeHandler;
    private readonly Command _okCommand;

    private Language _selectedLanguage;
    private Theme _selectedTheme;
    private bool _useNumpadLayout;
    private bool _useGlobalHotKeys;
    private bool _usePresetGroups;
    private bool _useCompactView;
    private bool _hasHotKeyConflicts;
    private string _hotKeyValidationMessage = string.Empty;
    private HotKeyBindingItemViewModel? _capturingHotKey;

    public OptionsViewModel(ISettingsService settings, IHotKeyService hotKeyService, Action closeHandler)
    {
        _settings = settings;
        _hotKeyService = hotKeyService;
        _closeHandler = closeHandler;

        _okCommand = new Command(ExecuteOk, () => !HasHotKeyConflicts);
        OkCommand = _okCommand;
        CancelCommand = new Command(ExecuteCancel);
        HotKeyCaptureCommand = new Command(ExecuteHotKeyCapture);
        HotKeyPreviewKeyDownCommand = new Command(ExecuteHotKeyPreviewKeyDown);
        LanguageItems = CreateLanguageItems();
        ThemeItems = CreateThemeItems();
        HotKeyBindings = new ObservableCollection<HotKeyBindingItemViewModel>(
            _hotKeyService.Bindings.Select(binding => new HotKeyBindingItemViewModel(binding)));

        _selectedLanguage = _settings.Language;
        _selectedTheme = _settings.Theme;
        _useNumpadLayout = _settings.UseNumpadLayout;
        _useGlobalHotKeys = _settings.UseGlobalHotKeys;
        _usePresetGroups = _settings.UsePresetGroups;
        _useCompactView = _settings.UseCompactView;

        RefreshHotKeyValidation();
    }

    public ICommand OkCommand { get; }

    public ICommand CancelCommand { get; }

    public ICommand HotKeyCaptureCommand { get; }

    public ICommand HotKeyPreviewKeyDownCommand { get; }

    public IEnumerable<LanguageItem> LanguageItems { get; }

    public IEnumerable<ThemeItem> ThemeItems { get; }

    public ObservableCollection<HotKeyBindingItemViewModel> HotKeyBindings { get; }

    public bool HasHotKeyConflicts
    {
        get => _hasHotKeyConflicts;
        private set
        {
            if (_hasHotKeyConflicts == value)
            {
                return;
            }

            _hasHotKeyConflicts = value;

            NotifyPropertyChanged();
            _okCommand.Invalidate();
        }
    }

    public string HotKeyValidationMessage
    {
        get => _hotKeyValidationMessage;
        private set
        {
            if (_hotKeyValidationMessage == value)
            {
                return;
            }

            _hotKeyValidationMessage = value;

            NotifyPropertyChanged();
        }
    }

    public Language SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            _selectedLanguage = value;

            NotifyPropertyChanged();
        }
    }

    public Theme SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            _selectedTheme = value;

            NotifyPropertyChanged();
        }
    }

    public bool UseNumpadLayout
    {
        get => _useNumpadLayout;
        set
        {
            _useNumpadLayout = value;

            NotifyPropertyChanged();
        }
    }

    public bool UseGlobalHotKeys
    {
        get => _useGlobalHotKeys;
        set
        {
            _useGlobalHotKeys = value;

            NotifyPropertyChanged();
        }
    }

    public bool UsePresetGroups
    {
        get => _usePresetGroups;
        set
        {
            _usePresetGroups = value;

            NotifyPropertyChanged();
        }
    }

    public bool UseCompactView
    {
        get => _useCompactView;
        set
        {
            _useCompactView = value;

            NotifyPropertyChanged();
        }
    }

    private void ExecuteOk()
    {
        RefreshHotKeyValidation();

        if (HasHotKeyConflicts)
        {
            return;
        }

        var selection = new OptionsSelection(
            Language: _selectedLanguage,
            Theme: _selectedTheme,
            UseCompactView: _useCompactView,
            UsePresetGroups: _usePresetGroups,
            UseNumpadLayout: _useNumpadLayout,
            UseGlobalHotKeys: _useGlobalHotKeys);

        _settings.ApplyOptions(selection);
        _hotKeyService.ApplyBindings([.. HotKeyBindings.Select(binding => binding.ToBinding())]);

        _closeHandler.Invoke();
    }

    private void ExecuteCancel()
    {
        _closeHandler.Invoke();
    }

    private void ExecuteHotKeyCapture(object? parameter)
    {
        _capturingHotKey?.IsCapturing = false;
        _capturingHotKey = parameter as HotKeyBindingItemViewModel;
        _capturingHotKey?.IsCapturing = true;
    }

    private void ExecuteHotKeyPreviewKeyDown(object? parameter)
    {
        if (_capturingHotKey is null || parameter is not KeyEventArgs eventArgs)
        {
            return;
        }

        var key = GetPressedKey(eventArgs);

        eventArgs.Handled = true;

        if (key == Key.Escape)
        {
            _capturingHotKey.IsCapturing = false;
            _capturingHotKey = null;
            return;
        }

        if (IsModifierKey(key))
        {
            return;
        }

        _capturingHotKey.Modifier = Keyboard.Modifiers;
        _capturingHotKey.Key = key;
        _capturingHotKey.IsCapturing = false;
        _capturingHotKey = null;

        RefreshHotKeyValidation();
    }

    private void RefreshHotKeyValidation()
    {
        foreach (var binding in HotKeyBindings)
        {
            binding.HasConflict = false;
        }

        var duplicateGestures = HotKeyBindings
            .Where(binding => binding.Key != Key.None)
            .GroupBy(binding => new { binding.Modifier, binding.Key })
            .Where(group => group.Count() > 1)
            .SelectMany(group => group)
            .ToHashSet();

        foreach (var binding in duplicateGestures)
        {
            binding.HasConflict = true;
        }

        var validation = _hotKeyService.ValidateBindings(HotKeyBindings.Select(binding => binding.ToBinding()));

        HasHotKeyConflicts = !validation.IsValid;
        HotKeyValidationMessage = validation.Message ?? string.Empty;
    }

    private static List<LanguageItem> CreateLanguageItems()
    {
        return [.. Enum.GetValues<Language>().Select(language => new LanguageItem(language))];
    }

    private static List<ThemeItem> CreateThemeItems()
    {
        return [.. Enum.GetValues<Theme>().Select(theme => new ThemeItem(theme))];
    }

    private static Key GetPressedKey(KeyEventArgs eventArgs)
    {
        if (eventArgs.Key == Key.System)
        {
            return eventArgs.SystemKey;
        }

        if (eventArgs.Key == Key.ImeProcessed)
        {
            return eventArgs.ImeProcessedKey;
        }

        return eventArgs.Key;
    }

    private static bool IsModifierKey(Key key) => key is
        Key.LeftCtrl or Key.RightCtrl or
        Key.LeftAlt or Key.RightAlt or
        Key.LeftShift or Key.RightShift or
        Key.LWin or Key.RWin;
}
