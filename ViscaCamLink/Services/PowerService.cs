namespace ViscaCamLink.Services;

using ViscaCamLink.Visca;
using ViscaCamLink.Visca.Types;

public sealed class PowerService(IViscaController viscaController) : IPowerService
{
    public event EventHandler<PowerStatus>? PowerStatusChanged;
    public event EventHandler? SwitchingPower;

    public async Task RefreshPowerStatusAsync()
    {
        var status = await viscaController.GetPowerStatus().ConfigureAwait(false);

        PowerStatusChanged?.Invoke(this, status);
    }

    public async Task SwitchPowerAsync()
    {
        var currentStatus = await viscaController.GetPowerStatus().ConfigureAwait(false);

        switch (currentStatus)
        {
            case PowerStatus.On:
                SwitchingPower?.Invoke(this, EventArgs.Empty);
                await viscaController.PowerOff().ConfigureAwait(false);
                break;
            case PowerStatus.Standby:
                SwitchingPower?.Invoke(this, EventArgs.Empty);
                await viscaController.PowerOn().ConfigureAwait(false);
                break;
            default:
                return;
        }

        var newStatus = await viscaController.GetUpdatedPowerStatus(currentStatus).ConfigureAwait(false);

        PowerStatusChanged?.Invoke(this, newStatus);
    }
}
