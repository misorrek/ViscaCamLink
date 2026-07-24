namespace ViscaCamLink.Infrastructure.Localization;

using System.Globalization;
using System.Threading;

using ViscaCamLink.Extensions;
using ViscaCamLink.Resources;

public static class LocalizationHelper
{
    public static void ApplyLocalization(Language language)
    {
        var culture = language == Language.System
            ? CultureInfo.CurrentCulture
            : CultureInfo.GetCultureInfo(language.GetDescription());

        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        Strings.Culture = culture;

        TranslationSource.Instance.NotifyLanguageChanged();
    }
}
