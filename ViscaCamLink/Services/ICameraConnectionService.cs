namespace ViscaCamLink.Services;

using ViscaCamLink.Repositories.AppSettings;
using ViscaCamLink.Visca.Types;

public interface ICameraConnectionService : IDisposable
{
    event EventHandler<ConnectionStatus>? ConnectionStatusChanged;

    ConnectionStatus Status { get; }

    bool IsSwitchingCameraProfile { get; }

    Task ReconnectAsync();

    Task SwitchCameraProfileAsync(CameraProfile camera);

    void CommitConnectionSettings(string ip, int port);
}
