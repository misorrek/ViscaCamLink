namespace ViscaCamLink.Services;

using ViscaCamLink.Repositories;
using ViscaCamLink.Util;

public interface ISettingsService
{
    Microsoft.Extensions.Logging.LogLevel LogLevel { get; }

    string Ip { get; set; }

    int Port { get; set; }

    bool MemoryContainerVisible { get; set; }

    bool MoveContainerVisible { get; set; }

    bool ZoomContainerVisible { get; set; }

    int PanTiltSpeed { get; set; }

    int ZoomSpeed { get; set; }

    Language Language { get; set; }

    bool NumpadLayout { get; set; }

    bool GlobalHotKeys { get; set; }

    bool UsePresetGroups { get; set; }

    WindowPlacementData? WindowPlacement { get; set; }

    void Save();

    void ApplyOptions(Language language, bool numpadLayout, bool globalHotKeys, bool usePresetGroups);
}
