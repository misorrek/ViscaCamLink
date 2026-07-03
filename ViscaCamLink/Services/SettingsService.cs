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

    public string Ip
    {
        get => settings.Ip;
        set => settings.Ip = value;
    }

    public int Port
    {
        get => settings.Port;
        set => settings.Port = value;
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

    public void Save() => settingsRepository.Save(settings);

    public void ApplyOptions(Language language, bool numpadLayout, bool globalHotKeys, bool usePresetGroups)
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
    }
}
