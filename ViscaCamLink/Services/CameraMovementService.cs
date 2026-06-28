namespace ViscaCamLink.Services;

using ViscaCamLink.Visca;
using ViscaCamLink.Visca.Types;

public sealed class CameraMovementService(IViscaController viscaController) : ICameraMovementService
{
    public int MaxPanTiltSpeed => viscaController.MaxPanSpeed;

    public int MaxZoomSpeed => viscaController.MaxZoomSpeed;

    public byte GetProportionalTiltSpeed(int panTiltSpeed)
    {
        var speedInPercent = (double)panTiltSpeed / viscaController.MaxPanSpeed;

        return (byte)Math.Floor(viscaController.MaxTiltSpeed * speedInPercent);
    }

    public Task PanTiltAsync(PanTiltDirection panTiltDirection, byte panSpeed, byte tiltSpeed) =>
        viscaController.ContinuousPanTilt(panTiltDirection, panSpeed, tiltSpeed);

    public Task StopPanTiltAsync() =>
        viscaController.ContinuousPanTilt(PanTiltDirection.None, 0, 0);

    public Task ZoomAsync(ZoomDirection zoomDirection, byte speed) =>
        viscaController.ContinuousZoom(zoomDirection, speed);

    public Task GoHomeAsync() =>
        viscaController.GoHome();
}
