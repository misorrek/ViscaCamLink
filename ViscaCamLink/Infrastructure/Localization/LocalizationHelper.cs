namespace ViscaCamLink.Infrastructure.Localization;

using System.Globalization;
using System.Threading;

using ViscaCamLink.Extensions;
using ViscaCamLink.Resources;

public static class LocalizationHelper
{
    public static void ApplyLocalization(Language language)
    {
        CultureInfo cultureInfo;

        if (language == Language.System)
        {
            cultureInfo = CultureInfo.CurrentCulture;
        }
        else
        {
            cultureInfo = CultureInfo.CreateSpecificCulture(language.GetDescription());
        }

        Thread.CurrentThread.CurrentCulture = cultureInfo;
        Thread.CurrentThread.CurrentUICulture = cultureInfo;
        Strings.Culture = cultureInfo;

        TranslationSource.Instance.NotifyLanguageChanged();
    }
}
