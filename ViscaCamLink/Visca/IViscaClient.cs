namespace ViscaCamLink.Visca;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface IViscaClient : IDisposable
{
    bool? IsConnected();

    Task ReconnectAsync(string? host = null, int? port = null, CancellationToken cancellationToken = default);

    Task<ViscaPacket> SendAsync(ViscaPacket request, CancellationToken cancellationToken);
}
