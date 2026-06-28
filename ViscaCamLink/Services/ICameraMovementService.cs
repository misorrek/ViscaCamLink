using ViscaCamLink.Visca.Types;

namespace ViscaCamLink.Services;

public interface ICameraMovementService
{
    int MaxPanTiltSpeed { get; }

    int MaxZoomSpeed { get; }

    byte GetProportionalTiltSpeed(int panTiltSpeed);

    Task PanTiltAsync(PanTiltDirection panTiltDirection, byte panSpeed, byte tiltSpeed);

    Task StopPanTiltAsync();

    Task ZoomAsync(ZoomDirection zoomDirection, byte speed);

    Task GoHomeAsync();
}
