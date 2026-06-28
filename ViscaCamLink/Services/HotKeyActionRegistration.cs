using ViscaCamLink.Repositories;

namespace ViscaCamLink.Services;

public sealed record HotKeyActionRegistration(HotKeyAction Action, Action Callback);