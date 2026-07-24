namespace ViscaCamLink.Infrastructure.Theming;

using System;
using System.Linq;
using System.Windows;

using Microsoft.Win32;

public static class ThemeHelper
{
    private const string ThemeDictionaryUriFormat = "/ViscaCamLink;component/Resources/{0}Theme.xaml";

    public static void ApplyTheme(Theme theme)
    {
        var resolvedTheme = theme == Theme.System ? GetSystemTheme() : theme;
        var uri = new Uri(string.Format(ThemeDictionaryUriFormat, resolvedTheme), UriKind.Relative);
        var newDictionary = new ResourceDictionary { Source = uri };
        var mergedDictionaries = Application.Current.Resources.MergedDictionaries;
        var existingDictionary = mergedDictionaries.FirstOrDefault(dictionary =>
            dictionary.Source?.OriginalString.EndsWith("Theme.xaml", StringComparison.OrdinalIgnoreCase) == true);

        if (existingDictionary is not null)
        {
            mergedDictionaries.Remove(existingDictionary);
        }

        mergedDictionaries.Add(newDictionary);
    }

    private static Theme GetSystemTheme()
    {
        try
        {
            var value = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme",
                defaultValue: 1);

            return value is int useLightTheme && useLightTheme == 0 ? Theme.Dark : Theme.Light;
        }
        catch
        {
            // Missing or unreadable registry value — fall back to the light theme.
            return Theme.Light;
        }
    }
}
