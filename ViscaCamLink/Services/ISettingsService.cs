namespace ViscaCamLink.Services;

using ViscaCamLink.Infrastructure.Localization;
using ViscaCamLink.Repositories.AppSettings;

public interface ISettingsService
{
    Microsoft.Extensions.Logging.LogLevel LogLevel { get; }

    IReadOnlyList<CameraProfile> CameraProfiles { get; }

    CameraProfile? ActiveCameraProfile { get; }

    Guid ActiveCameraProfileId { get; }

    bool MemoryContainerVisible { get; set; }

    bool MoveContainerVisible { get; set; }

    bool ZoomContainerVisible { get; set; }

    int PanTiltSpeed { get; set; }

    int ZoomSpeed { get; set; }

    Language Language { get; set; }

    bool NumpadLayout { get; set; }

    bool GlobalHotKeys { get; set; }

    bool UsePresetGroups { get; set; }

    bool UseMultipleCameraProfiles { get; set; }

    WindowPlacementData? WindowPlacement { get; set; }

    bool MinimizeToCompactWindow { get; set; }

    void AddCameraProfile(CameraProfile profile);

    void RemoveCameraProfile(Guid id);

    void UpdateCameraProfile(CameraProfile profile);

    void SetActiveCameraProfile(Guid id);

    void Save();

    void ApplyOptions(Language language, bool numpadLayout, bool globalHotKeys, bool usePresetGroups, bool useMultipleCameras, bool minimizeToCompactWindow);
}
