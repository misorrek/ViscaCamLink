namespace ViscaCamLink.Services;

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
        settings.CameraProfiles.FirstOrDefault(c => c.Id == settings.ActiveCameraProfileId)
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
        if (settings.CameraProfiles.Any(c => c.Id == profile.Id))
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
        var profileToDelete = settings.CameraProfiles.FirstOrDefault(c => c.Id == id);

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
        var index = settings.CameraProfiles.FindIndex(c => c.Id == profile.Id);

        if (index < 0)
        {
            return;
        }

        settings.CameraProfiles[index] = profile;

        Save();
    }

    public void SetActiveCameraProfile(Guid id)
    {
        if (settings.CameraProfiles.All(c => c.Id != id))
        {
            return;
        }

        settings.ActiveCameraProfileId = id;

        Save();
    }

    public void Save() => settingsRepository.Save(settings);

    // TODO: Create options model with all these settings that this service and the callers of this method use
    public void ApplyOptions(Language language, bool numpadLayout, bool globalHotKeys, bool usePresetGroups, bool minimizeToCompactWindow, Theme theme)
    {
        if (!Language.Equals(language))
        {
            Language = language;

            Save();
            applyLocalization(language);
        }

        if (UseNumpadLayout != numpadLayout)
        {
            UseNumpadLayout = numpadLayout;

            Save();
        }

        if (UseGlobalHotKeys != globalHotKeys)
        {
            UseGlobalHotKeys = globalHotKeys;

            Save();
        }

        if (UsePresetGroups != usePresetGroups)
        {
            UsePresetGroups = usePresetGroups;

            Save();
        }

        if (UseCompactView != minimizeToCompactWindow)
        {
            UseCompactView = minimizeToCompactWindow;

            Save();
        }

        if (Theme != theme)
        {
            Theme = theme;

            Save();
            applyTheme(theme);
        }
    }
}
