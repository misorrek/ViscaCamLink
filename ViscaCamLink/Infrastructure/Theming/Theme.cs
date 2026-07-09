namespace ViscaCamLink.Infrastructure.Theming;

using System;

using ViscaCamLink.Resources;

[Serializable]
public enum Theme
{
    System = 0,
    Light = 1,
    Dark = 2,
}

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
