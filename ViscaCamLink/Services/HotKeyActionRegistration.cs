using ViscaCamLink.Repositories.HotKeys;

namespace ViscaCamLink.Services;

public sealed record HotKeyActionRegistration(HotKeyAction Action, Action Callback, Action? ReleaseCallback = null);