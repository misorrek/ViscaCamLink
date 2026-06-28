namespace ViscaCamLink.Services;

using System.Windows.Input;

public interface IHotKeyService
{
    IReadOnlyList<HotKeyBinding> Bindings { get; }

    void RegisterActions(IEnumerable<HotKeyActionRegistration> registrations);

    bool RegisterHotKey(ModifierKeys modifier, Key key, Action action);

    HotKeyBindingValidationResult ValidateBindings(IEnumerable<HotKeyBinding> bindings);

    bool ApplyBindings(IReadOnlyList<HotKeyBinding> bindings);
}
