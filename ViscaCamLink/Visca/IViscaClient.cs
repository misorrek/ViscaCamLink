namespace ViscaCamLink.Visca;

public interface IViscaClient : IDisposable
{
    bool? IsConnected();

    Task Reconnect(CancellationToken cancellationToken, string? host = null, int? port = null);

    Task<ViscaPacket> SendAsync(ViscaPacket request, CancellationToken cancellationToken);
}
