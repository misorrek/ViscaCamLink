namespace ViscaCamLink.Services;

using ViscaCamLink.Updater;

public sealed class UpdateService : IUpdateService
{
    private readonly IGitHubUpdateChecker _checker;
    private readonly Version _currentVersion;

    public UpdateService(IGitHubUpdateChecker checker, Version currentVersion)
    {
        _checker = checker;
        _currentVersion = currentVersion;
    }

    public event EventHandler<UpdateInfo>? UpdateAvailable;

    public async Task StartAsync(CancellationToken ct = default)
    {
        var info = await _checker.CheckAsync(_currentVersion, ct).ConfigureAwait(false);

        if (info is not null)
            UpdateAvailable?.Invoke(this, info);
    }
}
