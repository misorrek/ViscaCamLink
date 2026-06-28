namespace ViscaCamLink.Services;

using ViscaCamLink.Updater;

public interface IUpdateService
{
    event EventHandler<UpdateInfo>? UpdateAvailable;

    Task StartAsync(CancellationToken ct = default);
}
