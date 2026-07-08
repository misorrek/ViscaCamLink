namespace ViscaCamLink.Services;

using ViscaCamLink.Infrastructure.Localization;
using ViscaCamLink.Repositories.AppSettings;

public sealed class SettingsService(AppSettings settings, AppSettingsRepository settingsRepository, Action<Language> applyLocalization) : ISettingsService
{
    public Microsoft.Extensions.Logging.LogLevel LogLevel
    {
        get => settings.LogLevel;
        set => settings.LogLevel = value;
    }

        public Language Language
    {
        get => settings.Language;
        set => settings.Language = value;
    }

    public IReadOnlyList<CameraProfile> CameraProfiles => settings.CameraProfiles;

    public CameraProfile? ActiveCameraProfile =>
        settings.CameraProfiles.FirstOrDefault(c => c.Id == settings.ActiveCameraProfileId)
        ?? settings.CameraProfiles.FirstOrDefault();

    public Guid ActiveCameraProfileId => ActiveCameraProfile?.Id ?? Guid.Empty;

    public bool UseMultipleCameraProfiles
    {
        get => settings.UseMultipleCameraProfiles;
        set => settings.UseMultipleCameraProfiles = value;
    }

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

    public bool NumpadLayout
    {
        get => settings.NumpadLayout;
        set => settings.NumpadLayout = value;
    }

    public bool GlobalHotKeys
    {
        get => settings.GlobalHotKeys;
        set => settings.GlobalHotKeys = value;
    }

    public bool UsePresetGroups
    {
        get => settings.UsePresetGroups;
        set => settings.UsePresetGroups = value;
    }

    public WindowPlacementData? WindowPlacement
    {
        get => settings.WindowPlacement;
        set => settings.WindowPlacement = value;
    }

    public bool MinimizeToCompactWindow
    {
        get => settings.MinimizeToCompactWindow;
        set => settings.MinimizeToCompactWindow = value;
    }

    // TODO: Check if id is still in the collection?
    public void AddCameraProfile(CameraProfile profile)
    {
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

    public void ApplyOptions(Language language, bool numpadLayout, bool globalHotKeys, bool usePresetGroups, bool useMultipleCameraProfiles, bool minimizeToCompactWindow)
    {
        if (!Language.Equals(language))
        {
            Language = language;

            Save();
            applyLocalization(language);
        }

        if (NumpadLayout != numpadLayout)
        {
            NumpadLayout = numpadLayout;

            Save();
        }

        if (GlobalHotKeys != globalHotKeys)
        {
            GlobalHotKeys = globalHotKeys;

            Save();
        }

        if (UsePresetGroups != usePresetGroups)
        {
            UsePresetGroups = usePresetGroups;

            Save();
        }

        if (UseMultipleCameraProfiles != useMultipleCameraProfiles)
        {
            UseMultipleCameraProfiles = useMultipleCameraProfiles;

            Save();
        }

        if (MinimizeToCompactWindow != minimizeToCompactWindow)
        {
            MinimizeToCompactWindow = minimizeToCompactWindow;

            Save();
        }
    }
}
