namespace ViscaCamLink.Extensions;

using ViscaCamLink.Infrastructure.Theming;
using ViscaCamLink.Resources;

public static class ThemeExtension
{
    public static string ToLocalizedString(this Theme theme)
    {
        return theme switch
        {
            Theme.System => Strings.Theme_System,
            Theme.Light => Strings.Theme_Light,
            Theme.Dark => Strings.Theme_Dark,
            _ => theme.ToString(),
        };
    }
}
