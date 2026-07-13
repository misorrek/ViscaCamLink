namespace ViscaCamLink.Repositories.AppSettings;

using Microsoft.Extensions.Logging;

using ViscaCamLink.Infrastructure.Localization;
using ViscaCamLink.Infrastructure.Theming;

public sealed class AppSettings
{
    public WindowPlacementData? WindowPlacement { get; set; }

    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    public Language Language { get; set; } = Language.System;

    public Theme Theme { get; set; } = Theme.System;

    public List<CameraProfile> CameraProfiles { get; set; } = [];

    public Guid ActiveCameraProfileId { get; set; } = Guid.Empty;

    public bool ConnectionContainerVisible { get; set; } = true;

    public bool MemoryContainerVisible { get; set; } = true;

    public bool MoveContainerVisible { get; set; } = true;

    public bool ZoomContainerVisible { get; set; } = true;

    public int PanTiltSpeed { get; set; } = 1;

    public int ZoomSpeed { get; set; } = 1;

    public bool UseMultipleCameraProfiles { get; set; } = false;

    public bool UseCompactView { get; set; } = true;

    public bool UsePresetGroups { get; set; } = true;

    public bool UseNumpadLayout { get; set; } = true;

    public bool UseGlobalHotKeys { get; set; } = true;

    public bool WixUninstallPrompted { get; set; } = false;
}