namespace ViscaCamLink.Services;

using System;
using System.Threading.Tasks;

using ViscaCamLink.Visca;
using ViscaCamLink.Visca.Types;

public class CameraMovementService(IViscaController viscaController) : ICameraMovementService
{
    public int MaxPanTiltSpeed => viscaController.MaxPanSpeed;

    public int MaxProportionalTiltSpeed => viscaController.MaxTiltSpeed;

    public int MaxZoomSpeed => viscaController.MaxZoomSpeed;

    public byte GetProportionalTiltSpeed(int panTiltSpeed)
    {
        var speedInPercent = (double)panTiltSpeed / viscaController.MaxPanSpeed;

        return (byte)Math.Floor(viscaController.MaxTiltSpeed * speedInPercent);
    }

    public Task PanTiltAsync(PanTiltDirection panTiltDirection, byte panSpeed, byte tiltSpeed) =>
        viscaController.ContinuousPanTiltAsync(panTiltDirection, panSpeed, tiltSpeed);

    public Task StopPanTiltAsync() =>
        viscaController.ContinuousPanTiltAsync(PanTiltDirection.None, 0, 0);

    public Task ZoomAsync(ZoomDirection zoomDirection, byte speed) =>
        viscaController.ContinuousZoomAsync(zoomDirection, speed);

    public Task GoHomeAsync() =>
        viscaController.GoHomeAsync();
}
