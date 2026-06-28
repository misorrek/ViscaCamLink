namespace ViscaCamLink.Services;

using System.Windows.Input;

using ViscaCamLink.Util;

public sealed class HotKeyService : IHotKeyService, IDisposable
{
    private readonly IGlobalHotKeyManager _hotKeyManager;
    private readonly IHotKeyRepository _repository;
    private readonly Dictionary<HotKeyAction, Action> _actions = [];
    private List<HotKeyBinding> _bindings;

    public HotKeyService(IGlobalHotKeyManager hotKeyManager, IHotKeyRepository repository)
    {
        _hotKeyManager = hotKeyManager;
        _repository = repository;
        _bindings = _repository.Load().Select(binding => binding.Copy()).ToList();
    }

    public IReadOnlyList<HotKeyBinding> Bindings => _bindings
        .Select(binding => binding.Copy())
        .ToList();

    public void RegisterActions(IEnumerable<HotKeyActionRegistration> registrations)
    {
        ArgumentNullException.ThrowIfNull(registrations);

        _actions.Clear();
        foreach (var registration in registrations)
        {
            _actions[registration.Action] = registration.Callback;
        }

        RegisterConfiguredBindings();
    }

    public bool RegisterHotKey(ModifierKeys modifier, Key key, Action action) =>
        _hotKeyManager.RegisterHotKey(modifier, key, action);

    public HotKeyBindingValidationResult ValidateBindings(IEnumerable<HotKeyBinding> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);

        var bindingList = bindings.ToList();
        if (bindingList.Any(binding => binding.Key == Key.None))
        {
            return HotKeyBindingValidationResult.Invalid("Each action needs a key assignment.");
        }

        var duplicateAction = bindingList
            .GroupBy(binding => binding.Action)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateAction is not null)
        {
            return HotKeyBindingValidationResult.Invalid($"{HotKeyDefinitions.GetDisplayName(duplicateAction.Key)} is listed more than once.");
        }

        var duplicateGesture = bindingList
            .GroupBy(binding => new { binding.Modifier, binding.Key })
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateGesture is not null)
        {
            return HotKeyBindingValidationResult.Invalid($"{FormatGesture(duplicateGesture.Key.Modifier, duplicateGesture.Key.Key)} is assigned more than once.");
        }

        return HotKeyBindingValidationResult.Valid();
    }

    public bool ApplyBindings(IReadOnlyList<HotKeyBinding> bindings)
    {
        var validation = ValidateBindings(bindings);
        if (!validation.IsValid)
        {
            return false;
        }

        _bindings = bindings.Select(binding => binding.Copy()).ToList();
        _repository.Save(_bindings);
        RegisterConfiguredBindings();

        return true;
    }

    public void Dispose() => _hotKeyManager.Dispose();

    private void RegisterConfiguredBindings()
    {
        _hotKeyManager.UnregisterAll();

        var validation = ValidateBindings(_bindings);
        if (!validation.IsValid)
        {
            _bindings = HotKeyDefinitions.CreateDefaultBindings().Select(binding => binding.Copy()).ToList();
        }

        foreach (var binding in _bindings)
        {
            if (_actions.TryGetValue(binding.Action, out var action))
            {
                _hotKeyManager.RegisterHotKey(binding.Modifier, binding.Key, action);
            }
        }
    }

    private static string FormatGesture(ModifierKeys modifier, Key key)
    {
        return modifier == ModifierKeys.None
            ? key.ToString()
            : $"{modifier}+{key}";
    }
}
