namespace ViscaCamLink.Services;

using System;
using System.Threading.Tasks;

using ViscaCamLink.Visca;
using ViscaCamLink.Visca.Types;

public class PowerService(IViscaController viscaController) : IPowerService
{
    public event EventHandler<PowerStatus>? PowerStatusChanged;

    public event EventHandler? SwitchingPower;

    public async Task RefreshPowerStatusAsync()
    {
        var status = await viscaController.GetPowerStatusAsync().ConfigureAwait(false);

        PowerStatusChanged?.Invoke(this, status);
    }

    public async Task SwitchPowerAsync()
    {
        var currentStatus = await viscaController.GetPowerStatusAsync().ConfigureAwait(false);

        switch (currentStatus)
        {
            case PowerStatus.On:
                SwitchingPower?.Invoke(this, EventArgs.Empty);
                await viscaController.PowerOffAsync().ConfigureAwait(false);
                break;
            case PowerStatus.Standby:
                SwitchingPower?.Invoke(this, EventArgs.Empty);
                await viscaController.PowerOnAsync().ConfigureAwait(false);
                break;
            default:
                return;
        }

        var newStatus = await viscaController.GetUpdatedPowerStatusAsync(currentStatus).ConfigureAwait(false);

        PowerStatusChanged?.Invoke(this, newStatus);
    }
}
