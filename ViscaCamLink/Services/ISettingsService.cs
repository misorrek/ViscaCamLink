namespace ViscaCamLink.Services;

using Microsoft.Extensions.Logging;

using ViscaCamLink.Infrastructure.Localization;
using ViscaCamLink.Infrastructure.Theming;
using ViscaCamLink.Repositories.AppSettings;

public interface ISettingsService
{
    WindowPlacementData? WindowPlacement { get; set; }

    LogLevel LogLevel { get; }

    Language Language { get; set; }

    Theme Theme { get; set; }

    IReadOnlyList<CameraProfile> CameraProfiles { get; }

    CameraProfile? ActiveCameraProfile { get; }

    Guid ActiveCameraProfileId { get; }

    bool ConnectionContainerVisible { get; set; }

    bool MemoryContainerVisible { get; set; }

    bool MoveContainerVisible { get; set; }

    bool ZoomContainerVisible { get; set; }

    int PanTiltSpeed { get; set; }

    int ZoomSpeed { get; set; }

    bool UseMultipleCameraProfiles { get; set; }

    bool UseCompactView { get; set; }

    bool UsePresetGroups { get; set; }

    bool UseNumpadLayout { get; set; }

    bool UseGlobalHotKeys { get; set; }

    void AddCameraProfile(CameraProfile profile);

    void RemoveCameraProfile(Guid id);

    void UpdateCameraProfile(CameraProfile profile);

    void SetActiveCameraProfile(Guid id);

    void Save();

    void ApplyOptions(Language language, bool numpadLayout, bool globalHotKeys, bool usePresetGroups, bool useMultipleCameras, bool minimizeToCompactWindow, Theme theme);
}
