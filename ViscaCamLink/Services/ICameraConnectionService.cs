namespace ViscaCamLink.Services;

using ViscaCamLink.Visca.Types;

public interface ICameraConnectionService : IDisposable
{
    event EventHandler<ConnectionStatus>? ConnectionStatusChanged;

    ConnectionStatus Status { get; }

    Task ReconnectAsync();

    void CommitConnectionSettings(string ip, int port);
}
