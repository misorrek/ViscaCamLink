namespace ViscaCamLink.Repositories.HotKeys;

using System.Windows.Input;
using ViscaCamLink.Util;

public static class HotKeyDefinitions
{
    public static IReadOnlyList<HotKeyAction> PresetActions { get; } =
    [
        HotKeyAction.Preset0,
        HotKeyAction.Preset1,
        HotKeyAction.Preset2,
        HotKeyAction.Preset3,
        HotKeyAction.Preset4,
        HotKeyAction.Preset5,
        HotKeyAction.Preset6,
        HotKeyAction.Preset7,
        HotKeyAction.Preset8,
        HotKeyAction.Preset9,
    ];

    public static IReadOnlyList<HotKeyAction> MovementActions { get; } =
    [
        HotKeyAction.MoveUp,
        HotKeyAction.MoveDown,
        HotKeyAction.MoveLeft,
        HotKeyAction.MoveRight,
        HotKeyAction.ZoomIn,
        HotKeyAction.ZoomOut,
    ];

    public static IReadOnlyList<HotKeyBinding> CreateDefaultBindings() =>
    [
        CreateDefault(HotKeyAction.Preset0, Key.NumPad0),
        CreateDefault(HotKeyAction.Preset1, Key.NumPad1),
        CreateDefault(HotKeyAction.Preset2, Key.NumPad2),
        CreateDefault(HotKeyAction.Preset3, Key.NumPad3),
        CreateDefault(HotKeyAction.Preset4, Key.NumPad4),
        CreateDefault(HotKeyAction.Preset5, Key.NumPad5),
        CreateDefault(HotKeyAction.Preset6, Key.NumPad6),
        CreateDefault(HotKeyAction.Preset7, Key.NumPad7),
        CreateDefault(HotKeyAction.Preset8, Key.NumPad8),
        CreateDefault(HotKeyAction.Preset9, Key.NumPad9),
        CreateDefault(HotKeyAction.MoveUp, Key.None),
        CreateDefault(HotKeyAction.MoveDown, Key.None),
        CreateDefault(HotKeyAction.MoveLeft, Key.None),
        CreateDefault(HotKeyAction.MoveRight, Key.None),
        CreateDefault(HotKeyAction.ZoomIn, Key.None),
        CreateDefault(HotKeyAction.ZoomOut, Key.None),
    ];

    public static bool TryGetPresetPosition(HotKeyAction action, out byte presetPosition)
    {
        var position = (int)action;

        if (position < 0 || position >= PresetActions.Count)
        {
            presetPosition = 0;
            return false;
        }

        presetPosition = (byte)position;
        return true;
    }

    public static string GetDisplayName(HotKeyAction action)
    {
        if (TryGetPresetPosition(action, out var presetPosition))
        {
            return string.Format(TranslationSource.Instance["HotKeyAction_Preset"], presetPosition);
        }

        return action switch
        {
            HotKeyAction.MoveUp => TranslationSource.Instance["HotKeyAction_MoveUp"],
            HotKeyAction.MoveDown => TranslationSource.Instance["HotKeyAction_MoveDown"],
            HotKeyAction.MoveLeft => TranslationSource.Instance["HotKeyAction_MoveLeft"],
            HotKeyAction.MoveRight => TranslationSource.Instance["HotKeyAction_MoveRight"],
            HotKeyAction.ZoomIn => TranslationSource.Instance["HotKeyAction_ZoomIn"],
            HotKeyAction.ZoomOut => TranslationSource.Instance["HotKeyAction_ZoomOut"],
            _ => action.ToString(),
        };
    }

    private static HotKeyBinding CreateDefault(HotKeyAction action, Key key) => new()
    {
        Action = action,
        Modifier = ModifierKeys.None,
        Key = key,
    };
}