namespace ViscaCamLink.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

using ViscaCamLink.Infrastructure.HotKeys;
using ViscaCamLink.Repositories.HotKeys;

public sealed class HotKeyService(
    IHotKeyManager hotKeyManager,
    IHotKeyRepository repository,
    ISettingsService settings) : IHotKeyService, IDisposable
{
    private readonly Dictionary<HotKeyAction, Action> _actions = [];
    private readonly Dictionary<HotKeyAction, Action> _releaseActions = [];

    private List<HotKeyBinding> _bindings = [.. repository.Load().Select(binding => binding.Copy())];

    public IReadOnlyList<HotKeyBinding> Bindings => [.. _bindings.Select(binding => binding.Copy())];

    public void Dispose() => hotKeyManager.Dispose();

    public void RegisterActions(IEnumerable<HotKeyActionRegistration> registrations)
    {
        ArgumentNullException.ThrowIfNull(registrations);

        foreach (var registration in registrations)
        {
            _actions[registration.Action] = registration.Callback;

            if (registration.ReleaseCallback is not null)
            {
                _releaseActions[registration.Action] = registration.ReleaseCallback;
            }
            else
            {
                _releaseActions.Remove(registration.Action);
            }
        }

        RegisterConfiguredBindings();
    }

    public bool RegisterHotKey(ModifierKeys modifier, Key key, Action action) =>
        hotKeyManager.RegisterHotKey(modifier, key, action);

    public HotKeyBindingValidationResult ValidateBindings(IEnumerable<HotKeyBinding> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);

        var bindingList = bindings.ToList();

        var duplicateAction = bindingList
            .GroupBy(binding => binding.Action)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateAction is not null)
        {
            return HotKeyBindingValidationResult.Invalid($"{HotKeyDefinitions.GetDisplayName(duplicateAction.Key)} is listed more than once.");
        }

        var duplicateGesture = bindingList
            .Where(binding => binding.Key != Key.None)
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

        _bindings = [.. bindings.Select(binding => binding.Copy())];

        repository.Save(_bindings);
        RegisterConfiguredBindings();

        return true;
    }

    private void RegisterConfiguredBindings()
    {
        hotKeyManager.UseGlobalHotKeys = settings.UseGlobalHotKeys;
        hotKeyManager.UnregisterAll();

        var validation = ValidateBindings(_bindings);

        if (!validation.IsValid)
        {
            _bindings = [.. HotKeyDefinitions.CreateDefaultBindings().Select(binding => binding.Copy())];
        }

        foreach (var binding in _bindings)
        {
            if (binding.Key == Key.None)
            {
                continue;
            }

            if (!_actions.TryGetValue(binding.Action, out var action))
            {
                continue;
            }

            if (_releaseActions.TryGetValue(binding.Action, out var releaseAction))
            {
                hotKeyManager.RegisterHoldHotKey(binding.Modifier, binding.Key, action, releaseAction);
            }
            else
            {
                hotKeyManager.RegisterHotKey(binding.Modifier, binding.Key, action);
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
