namespace ViscaCamLink.Repositories.AppSettings;

using Microsoft.Extensions.Logging;

using ViscaCamLink.Infrastructure.Localization;

public sealed class AppSettings
{
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    public Language Language { get; set; } = Language.System;

    public List<CameraProfile> CameraProfiles { get; set; } = [];

    public Guid ActiveCameraProfileId { get; set; } = Guid.Empty;

    public bool MemoryContainerVisible { get; set; } = true;

    public bool MoveContainerVisible { get; set; } = true;

    public bool ZoomContainerVisible { get; set; } = true;

    public int PanTiltSpeed { get; set; } = 1;

    public int ZoomSpeed { get; set; } = 1;

    public bool NumpadLayout { get; set; } = true;

    public bool GlobalHotKeys { get; set; } = true;

    public bool UsePresetGroups { get; set; } = true;

    public bool UseMultipleCameraProfiles { get; set; } = false;

    public WindowPlacementData? WindowPlacement { get; set; }

    public bool WixUninstallPrompted { get; set; } = false;
}