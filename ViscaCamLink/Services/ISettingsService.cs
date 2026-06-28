namespace ViscaCamLink.Services;

using ViscaCamLink.Util;

public interface ISettingsService
{
    string Ip { get; set; }

    int Port { get; set; }

    bool MemoryContainerVisible { get; set; }

    bool MoveContainerVisible { get; set; }

    bool ZoomContainerVisible { get; set; }

    int PanTiltSpeed { get; set; }

    int ZoomSpeed { get; set; }

    Language Language { get; set; }

    bool NumpadLayout { get; set; }

    void Save();

    void ApplyOptions(Language language, bool numpadLayout);
}
