namespace ViscaCamLink.Infrastructure.Theming;

using System;
using System.Linq;
using System.Windows;

using Microsoft.Win32;

public static class ThemeHelper
{
    private const string ThemeDictUriFormat = "/ViscaCamLink;component/Resources/{0}Theme.xaml";

    public static void ApplyTheme(Theme theme)
    {
        var resolvedTheme = theme == Theme.System ? GetSystemTheme() : theme;
        var uri = new Uri(string.Format(ThemeDictUriFormat, resolvedTheme), UriKind.Relative);
        var newDict = new ResourceDictionary { Source = uri };
        var merged = Application.Current.Resources.MergedDictionaries;
        var existing = merged.FirstOrDefault(d =>
            d.Source?.OriginalString.EndsWith("Theme.xaml", StringComparison.OrdinalIgnoreCase) == true);

        if (existing is not null)
        {
            merged.Remove(existing);
        }         

        merged.Add(newDict);
    }

    private static Theme GetSystemTheme()
    {
        try
        {
            var value = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme",
                defaultValue: 1);

            return value is int i && i == 0 ? Theme.Dark : Theme.Light;
        }
        catch
        {
            return Theme.Light;
        }
    }
}
