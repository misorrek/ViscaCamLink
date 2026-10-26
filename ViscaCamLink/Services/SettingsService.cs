namespace ViscaCamLink.Services;

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;

using ViscaCamLink.Infrastructure.Localization;
using ViscaCamLink.Infrastructure.Theming;
using ViscaCamLink.Repositories.AppSettings;

public partial class SettingsService(
    AppSettings settings,
    AppSettingsRepository settingsRepository,
    Action<Language> applyLocalization,
    Action<Theme> applyTheme,
    Action<LogLevel> applyLogLevel,
    ILogger<SettingsService> logger)
    : ISettingsService
{
    public WindowPlacementData? WindowPlacement
    {
        get => settings.WindowPlacement;
        set => settings.WindowPlacement = value;
    }

    public LogLevel LogLevel
    {
        get => settings.LogLevel;
        set => settings.LogLevel = value;
    }

    public Language Language
    {
        get => settings.Language;
        set => settings.Language = value;
    }

    public Theme Theme
    {
        get => settings.Theme;
        set => settings.Theme = value;
    }

    public IReadOnlyList<CameraProfile> CameraProfiles => settings.CameraProfiles;

    public CameraProfile? ActiveCameraProfile =>
        settings.CameraProfiles.FirstOrDefault(profile => profile.Id == settings.ActiveCameraProfileId)
        ?? settings.CameraProfiles.FirstOrDefault();

    public Guid ActiveCameraProfileId => ActiveCameraProfile?.Id ?? Guid.Empty;

    public bool ConnectionContainerVisible
    {
        get => settings.ConnectionContainerVisible;
        set => settings.ConnectionContainerVisible = value;
    }

    public bool MemoryContainerVisible
    {
        get => settings.MemoryContainerVisible;
        set => settings.MemoryContainerVisible = value;
    }

    public bool MoveContainerVisible
    {
        get => settings.MoveContainerVisible;
        set => settings.MoveContainerVisible = value;
    }

    public bool ZoomContainerVisible
    {
        get => settings.ZoomContainerVisible;
        set => settings.ZoomContainerVisible = value;
    }

    public int PanTiltSpeed
    {
        get => settings.PanTiltSpeed;
        set => settings.PanTiltSpeed = value;
    }

    public int ZoomSpeed
    {
        get => settings.ZoomSpeed;
        set => settings.ZoomSpeed = value;
    }

    public bool UseCompactView
    {
        get => settings.UseCompactView;
        set => settings.UseCompactView = value;
    }

    public bool UsePresetGroups
    {
        get => settings.UsePresetGroups;
        set => settings.UsePresetGroups = value;
    }

    public bool UseGlobalHotKeys
    {
        get => settings.UseGlobalHotKeys;
        set => settings.UseGlobalHotKeys = value;
    }

    public bool UseNumpadLayout
    {
        get => settings.UseNumpadLayout;
        set => settings.UseNumpadLayout = value;
    }

    public IReadOnlyList<string> DismissedHints => settings.DismissedHints;

    public void AddCameraProfile(CameraProfile profile)
    {
        if (settings.CameraProfiles.Any(existing => existing.Id == profile.Id))
        {
            throw new InvalidOperationException($"Camera profile with id {profile.Id} already exists.");
        }

        settings.CameraProfiles.Add(profile);

        if (settings.ActiveCameraProfileId == Guid.Empty)
        {
            settings.ActiveCameraProfileId = profile.Id;
        }

        Save();
        LogCameraProfileAdded(logger, profile.Name, profile.Ip, profile.Port);
    }

    public void RemoveCameraProfile(Guid id)
    {
        var profileToDelete = settings.CameraProfiles.FirstOrDefault(profile => profile.Id == id);

        if (profileToDelete is null)
        {
            return;
        }

        settings.CameraProfiles.Remove(profileToDelete);

        if (settings.ActiveCameraProfileId == id)
        {
            settings.ActiveCameraProfileId = settings.CameraProfiles.FirstOrDefault()?.Id ?? Guid.Empty;
        }

        Save();
        LogCameraProfileRemoved(logger, profileToDelete.Name);
    }

    public void UpdateCameraProfile(CameraProfile profile)
    {
        var index = settings.CameraProfiles.FindIndex(existing => existing.Id == profile.Id);

        if (index < 0)
        {
            return;
        }

        settings.CameraProfiles[index] = profile;

        Save();
    }

    public void SetActiveCameraProfile(Guid id)
    {
        if (settings.CameraProfiles.All(profile => profile.Id != id))
        {
            return;
        }

        settings.ActiveCameraProfileId = id;

        Save();
    }

    public void DismissHint(string hintId)
    {
        if (settings.DismissedHints.Contains(hintId))
        {
            return;
        }

        settings.DismissedHints.Add(hintId);

        Save();
    }

    public void ResetDismissedHints()
    {
        if (settings.DismissedHints.Count == 0)
        {
            return;
        }

        settings.DismissedHints.Clear();

        Save();
    }

    public void Save() => settingsRepository.Save(settings);

    public void ApplyOptions(OptionsSelection selection)
    {
        if (!Language.Equals(selection.Language))
        {
            Language = selection.Language;

            Save();
            applyLocalization(selection.Language);
        }

        if (Theme != selection.Theme)
        {
            Theme = selection.Theme;

            Save();
            applyTheme(selection.Theme);
        }

        if (UseCompactView != selection.UseCompactView)
        {
            UseCompactView = selection.UseCompactView;

            Save();
        }

        if (UsePresetGroups != selection.UsePresetGroups)
        {
            UsePresetGroups = selection.UsePresetGroups;

            Save();
        }

        if (UseNumpadLayout != selection.UseNumpadLayout)
        {
            UseNumpadLayout = selection.UseNumpadLayout;

            Save();
        }

        if (UseGlobalHotKeys != selection.UseGlobalHotKeys)
        {
            UseGlobalHotKeys = selection.UseGlobalHotKeys;

            Save();
        }

        if (LogLevel != selection.LogLevel)
        {
            LogLevel = selection.LogLevel;

            Save();
            applyLogLevel(selection.LogLevel);
        }

        LogOptionsApplied(logger, Language, Theme, UseCompactView, UsePresetGroups, UseNumpadLayout, UseGlobalHotKeys, LogLevel);
    }

    [LoggerMessage(EventId = 2401, Level = LogLevel.Information, Message = "Options applied: language {Language}, theme {Theme}, compact view {UseCompactView}, preset groups {UsePresetGroups}, numpad layout {UseNumpadLayout}, global hotkeys {UseGlobalHotKeys}, log level {LogLevel}")]
    private static partial void LogOptionsApplied(
        ILogger logger,
        Language language,
        Theme theme,
        bool useCompactView,
        bool usePresetGroups,
        bool useNumpadLayout,
        bool useGlobalHotKeys,
        LogLevel logLevel);

    [LoggerMessage(EventId = 2402, Level = LogLevel.Information, Message = "Camera profile '{Name}' ({Ip}:{Port}) added")]
    private static partial void LogCameraProfileAdded(ILogger logger, string name, string ip, int port);

    [LoggerMessage(EventId = 2403, Level = LogLevel.Information, Message = "Camera profile '{Name}' removed")]
    private static partial void LogCameraProfileRemoved(ILogger logger, string name);
}
