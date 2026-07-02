namespace ViscaCamLink.Infrastructure.Localization;

using System.ComponentModel;
using System.Windows.Data;

using ViscaCamLink.Resources;

public sealed class TranslationSource : INotifyPropertyChanged
{
    public static TranslationSource Instance { get; } = new();

    public string this[string key] => Strings.ResourceManager.GetString(key, Strings.Culture) ?? key;

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? LanguageChanged;

    public void NotifyLanguageChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Binding.IndexerName));
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }
}
