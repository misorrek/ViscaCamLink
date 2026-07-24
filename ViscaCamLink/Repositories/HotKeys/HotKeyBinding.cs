namespace ViscaCamLink.Repositories.HotKeys;

using System.Windows.Input;

public class HotKeyBinding
{
    public HotKeyAction Action { get; set; }

    public ModifierKeys Modifier { get; set; }

    public Key Key { get; set; }

    public HotKeyBinding Copy() => new()
    {
        Action = Action,
        Modifier = Modifier,
        Key = Key,
    };
}
