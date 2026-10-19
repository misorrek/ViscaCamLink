namespace ViscaCamLink.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

using Microsoft.Extensions.Logging;

using ViscaCamLink.Infrastructure.HotKeys;
using ViscaCamLink.Repositories.HotKeys;

public sealed partial class HotKeyService(
    IHotKeyManager hotKeyManager,
    IHotKeyRepository repository,
    ISettingsService settings,
    ILogger<HotKeyService> logger) : IHotKeyService, IDisposable
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

    public bool RegisterHotKey(ModifierKeys modifier, Key key, Action action)
    {
        var registered = hotKeyManager.RegisterHotKey(modifier, key, action);

        if (!registered)
        {
            LogHotKeyRegistrationFailed(logger, FormatGesture(modifier, key), null);
        }

        return registered;
    }

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
            LogInvalidBindingsReset(logger, validation.Message);

            _bindings = [.. HotKeyDefinitions.CreateDefaultBindings().Select(binding => binding.Copy())];
        }

        var registeredCount = 0;

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

            var registered = _releaseActions.TryGetValue(binding.Action, out var releaseAction)
                ? hotKeyManager.RegisterHoldHotKey(binding.Modifier, binding.Key, action, releaseAction)
                : hotKeyManager.RegisterHotKey(binding.Modifier, binding.Key, action);

            if (registered)
            {
                registeredCount++;
            }
            else
            {
                LogHotKeyRegistrationFailed(logger, FormatGesture(binding.Modifier, binding.Key), binding.Action);
            }
        }

        LogHotKeysRegistered(logger, registeredCount, settings.UseGlobalHotKeys);
    }

    private static string FormatGesture(ModifierKeys modifier, Key key)
    {
        return modifier == ModifierKeys.None
            ? key.ToString()
            : $"{modifier}+{key}";
    }

    [LoggerMessage(EventId = 2301, Level = LogLevel.Warning, Message = "Hotkey {Gesture} for action {Action} could not be registered; it may already be in use by another application")]
    private static partial void LogHotKeyRegistrationFailed(ILogger logger, string gesture, HotKeyAction? action);

    [LoggerMessage(EventId = 2302, Level = LogLevel.Warning, Message = "Configured hotkey bindings are invalid ({Reason}); falling back to default bindings")]
    private static partial void LogInvalidBindingsReset(ILogger logger, string? reason);

    [LoggerMessage(EventId = 2303, Level = LogLevel.Information, Message = "Registered {Count} hotkeys (global: {UseGlobalHotKeys})")]
    private static partial void LogHotKeysRegistered(ILogger logger, int count, bool useGlobalHotKeys);
}
