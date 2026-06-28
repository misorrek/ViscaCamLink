namespace ViscaCamLink.Services;

using ViscaCamLink.Properties;
using ViscaCamLink.Util;

public sealed class SettingsService(Action applyLocalization) : ISettingsService
{
    public string Ip
    {
        get => Settings.Default.Ip;
        set => Settings.Default.Ip = value;
    }

    public int Port
    {
        get => Settings.Default.Port;
        set => Settings.Default.Port = value;
    }

    public bool MemoryContainerVisible
    {
        get => Settings.Default.MemoryContainerVisible;
        set => Settings.Default.MemoryContainerVisible = value;
    }

    public bool MoveContainerVisible
    {
        get => Settings.Default.MoveContainerVisible;
        set => Settings.Default.MoveContainerVisible = value;
    }

    public bool ZoomContainerVisible
    {
        get => Settings.Default.ZoomContainerVisible;
        set => Settings.Default.ZoomContainerVisible = value;
    }

    public int PanTiltSpeed
    {
        get => Settings.Default.PanTiltSpeed;
        set => Settings.Default.PanTiltSpeed = value;
    }

    public int ZoomSpeed
    {
        get => Settings.Default.ZoomSpeed;
        set => Settings.Default.ZoomSpeed = value;
    }

    public Language Language
    {
        get => Settings.Default.Language;
        set => Settings.Default.Language = value;
    }

    public bool NumpadLayout
    {
        get => Settings.Default.NumpadLayout;
        set => Settings.Default.NumpadLayout = value;
    }

    public void Save() => Settings.Default.Save();

    public void ApplyOptions(Language language, bool numpadLayout)
    {
        if (!Language.Equals(language))
        {
            Language = language;

            Save();
            applyLocalization();
        }

        if (NumpadLayout != numpadLayout)
        {
            NumpadLayout = numpadLayout;

            Save();
        }
    }
}
