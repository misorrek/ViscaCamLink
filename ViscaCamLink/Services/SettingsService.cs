namespace ViscaCamLink.Services;

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Extensions.Logging;

using ViscaCamLink.Infrastructure.Localization;
using ViscaCamLink.Infrastructure.Theming;
using ViscaCamLink.Repositories.AppSettings;

public class SettingsService(
    AppSettings settings,
    AppSettingsRepository settingsRepository,
    Action<Language> applyLocalization,
    Action<Theme> applyTheme)
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
    }
}
