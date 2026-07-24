namespace ViscaCamLink.Services;

using System;

using ViscaCamLink.Repositories.HotKeys;

public record HotKeyActionRegistration(HotKeyAction Action, Action Callback, Action? ReleaseCallback = null);
