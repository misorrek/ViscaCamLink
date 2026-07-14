namespace ViscaCamLink.Infrastructure.Localization;

using System.Globalization;
using System.Threading;

using ViscaCamLink.Extensions;
using ViscaCamLink.Resources;

public static class LocalizationHelper
{
    private static readonly HashSet<string> SupportedResourceCultures = new(StringComparer.OrdinalIgnoreCase)
    {
        "en-US",
        "de-DE",
    };

    public static void ApplyLocalization(Language language)
    {
        CultureInfo selectedCulture;

        if (language == Language.System)
        {
            selectedCulture = CultureInfo.CurrentCulture;
        }
        else
        {
            selectedCulture = CultureInfo.CreateSpecificCulture(language.GetDescription());
        }

        var resourceCulture = GetNearestSupportedResourceCulture(selectedCulture);

        Thread.CurrentThread.CurrentCulture = selectedCulture;
        Thread.CurrentThread.CurrentUICulture = resourceCulture;
        Strings.Culture = resourceCulture;

        TranslationSource.Instance.NotifyLanguageChanged();
    }

    private static CultureInfo GetNearestSupportedResourceCulture(CultureInfo culture)
    {
        var current = culture;

        while (!current.Equals(CultureInfo.InvariantCulture))
        {
            if (SupportedResourceCultures.Contains(current.Name))
            {
                return current;
            }

            current = current.Parent;
        }

        return CultureInfo.InvariantCulture;
    }
}
