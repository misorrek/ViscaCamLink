namespace ViscaCamLink.ViewModels;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using ViscaCamLink.Properties;
using ViscaCamLink.Util;

public class OptionsViewModel : INotifyPropertyChanged
{
    public OptionsViewModel(Action closeHandler)
    {
        CloseHandler = closeHandler;

        OkCommand = new Command(ExecuteOk);
        CancelCommand = new Command(ExecuteCancel);
        LanguageItems = GetLanguageItems();

        _selectedLanguage = Settings.Default.Language;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand OkCommand { get; }

    public ICommand CancelCommand { get; }

    public IEnumerable<LanguageItem> LanguageItems { get; }

    public Language SelectedLanguage
    {
        get => _selectedLanguage;

        set
        {
            _selectedLanguage = value;

            NotifyPropertyChanged();
            NotifyPropertyChanged(nameof(LanguageChanged));
        }
    }

    public Boolean LanguageChanged => Settings.Default.Language != SelectedLanguage;
    private Action CloseHandler { get; }

    private Language _selectedLanguage;

    protected void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    //TODO Make options model
    private void ExecuteOk()
    {
        if (!Settings.Default.Language.Equals(_selectedLanguage))
        {
            Settings.Default.Language = _selectedLanguage;
            Settings.Default.Save();
        }

        CloseHandler.Invoke();
    }

    private void ExecuteCancel()
    {
        CloseHandler.Invoke();
    }

    private static IEnumerable<LanguageItem> GetLanguageItems()
    {
        var languages = new List<LanguageItem>();

        foreach (var language in Enum.GetValues<Language>())
        {
            languages.Add(new LanguageItem(language));
        }

        return languages;
    }
}

public class LanguageItem
{
    public LanguageItem(Language language)
    {
        LanguageValue = language;
    }

    public Language LanguageValue { get; }

    public String LanguageDisplay => LanguageValue.ToLocalizedString();
}
