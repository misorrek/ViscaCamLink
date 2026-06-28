using ViscaCamLink.Visca.Types;

namespace ViscaCamLink.Visca;

public interface IViscaController : IDisposable
{
    bool? Connected { get; }
    byte MaxPanSpeed { get; }
    byte MaxTiltSpeed { get; }
    byte MaxZoomSpeed { get; }

    Task Reconnect(CancellationToken cancellationToken, string? host = null, int? port = null);
    Task PowerOn(CancellationToken cancellationToken = default);
    Task PowerOff(CancellationToken cancellationToken = default);
    Task<PowerStatus> GetPowerStatus(CancellationToken cancellationToken = default);
    Task<PowerStatus> GetUpdatedPowerStatus(PowerStatus lastPowerStatus, CancellationToken cancellationToken = default);
    Task MemorySet(byte slot, CancellationToken cancellationToken = default);
    Task MemoryRecall(byte slot, CancellationToken cancellationToken = default);
    Task GoHome(CancellationToken cancellationToken = default);
    Task ContinuousPanTilt(PanTiltDirection panTiltDirection, byte panSpeed, byte tiltSpeed, CancellationToken cancellationToken = default);
    Task ContinuousZoom(ZoomDirection zoomDirection, byte zoomSpeed, CancellationToken cancellationToken = default);
}
