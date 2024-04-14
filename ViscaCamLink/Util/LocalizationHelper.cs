namespace ViscaCamLink.Util;

using System.Globalization;
using System.Threading;

using ViscaCamLink.Properties;

public static class LocalizationHelper
{
    public static void ApplyLocalization()
    {
        CultureInfo cultureInfo;

        if (Settings.Default.Language == Language.System)
        {
            cultureInfo = CultureInfo.CurrentCulture;
        }
        else
        {
            cultureInfo = CultureInfo.CreateSpecificCulture(Settings.Default.Language.GetDescription());
        }

        Thread.CurrentThread.CurrentCulture = cultureInfo;
        Thread.CurrentThread.CurrentUICulture = cultureInfo;
    }
}
