namespace ViscaCamLink.Services;

using System.Threading.Tasks;

using ViscaCamLink.Visca.Types;

public interface ICameraMovementService
{
    int MaxPanTiltSpeed { get; }

    int MaxProportionalTiltSpeed { get; }

    int MaxZoomSpeed { get; }

    byte GetProportionalTiltSpeed(int panTiltSpeed);

    Task PanTiltAsync(PanTiltDirection panTiltDirection, byte panSpeed, byte tiltSpeed);

    Task StopPanTiltAsync();

    Task ZoomAsync(ZoomDirection zoomDirection, byte speed);

    Task GoHomeAsync();
}
