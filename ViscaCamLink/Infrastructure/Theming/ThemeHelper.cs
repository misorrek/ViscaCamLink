namespace ViscaCamLink.Infrastructure.Theming;

using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Windows;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32;

public static partial class ThemeHelper
{
    private const string ThemeDictionaryUriFormat = "/ViscaCamLink;component/Resources/{0}Theme.xaml";
    private const string ThemeDictionarySuffix = "Theme.xaml";
    private const string PersonalizeKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValueName = "AppsUseLightTheme";

    private static ILogger _logger = NullLogger.Instance;
    private static bool _isFollowingSystemTheme;
    private static Theme? _appliedTheme;

    public static void ApplyTheme(Theme theme, ILogger logger)
    {
        _logger = logger;

        if (theme == Theme.System)
        {
            StartFollowingSystemTheme();
        }
        else
        {
            StopFollowingSystemTheme();
        }

        LoadThemeDictionary(theme == Theme.System ? GetSystemTheme(logger) : theme);
    }

    public static void StopFollowingSystemTheme()
    {
        if (!_isFollowingSystemTheme)
        {
            return;
        }

        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _isFollowingSystemTheme = false;
    }

    private static void StartFollowingSystemTheme()
    {
        if (_isFollowingSystemTheme)
        {
            return;
        }

        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        _isFollowingSystemTheme = true;
    }

    private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs eventArgs)
    {
        // Windows reports the light/dark app mode switch under the General category.
        if (eventArgs.Category != UserPreferenceCategory.General)
        {
            return;
        }

        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            if (!_isFollowingSystemTheme)
            {
                return;
            }

            var systemTheme = GetSystemTheme(_logger);

            if (systemTheme != _appliedTheme)
            {
                LogSystemThemeChanged(_logger, systemTheme);
                LoadThemeDictionary(systemTheme);
            }
        });
    }

    private static void LoadThemeDictionary(Theme resolvedTheme)
    {
        var uri = new Uri(string.Format(ThemeDictionaryUriFormat, resolvedTheme), UriKind.Relative);
        var newDictionary = new ResourceDictionary { Source = uri };
        var mergedDictionaries = Application.Current.Resources.MergedDictionaries;
        var existingDictionary = mergedDictionaries.FirstOrDefault(dictionary =>
            dictionary.Source?.OriginalString.EndsWith(ThemeDictionarySuffix, StringComparison.OrdinalIgnoreCase) == true);

        if (existingDictionary is not null)
        {
            mergedDictionaries.Remove(existingDictionary);
        }

        mergedDictionaries.Add(newDictionary);
        _appliedTheme = resolvedTheme;
    }

    private static Theme GetSystemTheme(ILogger logger)
    {
        try
        {
            var value = Registry.GetValue(PersonalizeKeyPath, AppsUseLightThemeValueName, defaultValue: 1);

            return value is int useLightTheme && useLightTheme == 0 ? Theme.Dark : Theme.Light;
        }
        catch (Exception exception) when (exception is SecurityException or IOException or UnauthorizedAccessException)
        {
            // Missing or unreadable registry value — fall back to the light theme.
            LogSystemThemeUnavailable(logger, exception);

            return Theme.Light;
        }
    }

    [LoggerMessage(EventId = 5101, Level = LogLevel.Debug, Message = "Windows app theme could not be read from the registry; using the light theme")]
    private static partial void LogSystemThemeUnavailable(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 5102, Level = LogLevel.Information, Message = "Windows app theme changed; applying the {Theme} theme")]
    private static partial void LogSystemThemeChanged(ILogger logger, Theme theme);
}
