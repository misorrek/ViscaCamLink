namespace ViscaCamLink.Visca;

using System;
using System.Threading;
using System.Threading.Tasks;

using ViscaCamLink.Visca.Types;

public interface IViscaController : IDisposable
{
    bool? Connected { get; }

    byte MaxPanSpeed { get; }

    byte MaxTiltSpeed { get; }

    byte MaxZoomSpeed { get; }

    Task ReconnectAsync(string? host = null, int? port = null, CancellationToken cancellationToken = default);

    Task PowerOnAsync(CancellationToken cancellationToken = default);

    Task PowerOffAsync(CancellationToken cancellationToken = default);

    Task<PowerStatus> GetPowerStatusAsync(CancellationToken cancellationToken = default);

    Task<PowerStatus> GetUpdatedPowerStatusAsync(PowerStatus lastPowerStatus, CancellationToken cancellationToken = default);

    Task MemorySetAsync(byte slot, CancellationToken cancellationToken = default);

    Task MemoryRecallAsync(byte slot, CancellationToken cancellationToken = default);

    Task GoHomeAsync(CancellationToken cancellationToken = default);

    Task ContinuousPanTiltAsync(PanTiltDirection panTiltDirection, byte panSpeed, byte tiltSpeed, CancellationToken cancellationToken = default);

    Task ContinuousZoomAsync(ZoomDirection zoomDirection, byte zoomSpeed, CancellationToken cancellationToken = default);
}
