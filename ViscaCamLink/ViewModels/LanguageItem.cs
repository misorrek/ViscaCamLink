namespace ViscaCamLink.ViewModels;

using ViscaCamLink.Extensions;
using ViscaCamLink.Infrastructure.Localization;

public class LanguageItem(Language language)
{
    public Language LanguageValue { get; } = language;

    public string LanguageDisplay => LanguageValue.ToLocalizedString();
}
