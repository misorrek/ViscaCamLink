namespace ViscaCamLink.Infrastructure.Localization;

using System.Globalization;
using System.Threading;

using ViscaCamLink.Resources;

public static class LocalizationHelper
{
    public static void ApplyLocalization(Language language)
    {
        var culture = language == Language.System
            ? CultureInfo.CurrentCulture
            : CultureInfo.GetCultureInfo(language.GetLanguageCode());

        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        Strings.Culture = culture;

        TranslationSource.Instance.NotifyLanguageChanged();
    }
}
