namespace ViscaCamLink.Services;

using ViscaCamLink.Visca;
using ViscaCamLink.Visca.Types;

public sealed class PowerService : IPowerService
{
    private readonly IViscaController _viscaController;

    public PowerService(IViscaController viscaController)
    {
        _viscaController = viscaController;
    }

    public event EventHandler<PowerStatus>? PowerStatusChanged;
    public event EventHandler? SwitchingPower;

    public async Task RefreshPowerStatusAsync()
    {
        var status = await _viscaController.GetPowerStatus().ConfigureAwait(false);
        PowerStatusChanged?.Invoke(this, status);
    }

    public async Task SwitchPowerAsync()
    {
        var currentStatus = await _viscaController.GetPowerStatus().ConfigureAwait(false);

        switch (currentStatus)
        {
            case PowerStatus.On:
                SwitchingPower?.Invoke(this, EventArgs.Empty);
                await _viscaController.PowerOff().ConfigureAwait(false);
                break;
            case PowerStatus.Standby:
                SwitchingPower?.Invoke(this, EventArgs.Empty);
                await _viscaController.PowerOn().ConfigureAwait(false);
                break;
            default:
                return;
        }

        var newStatus = await _viscaController.GetUpdatedPowerStatus(currentStatus).ConfigureAwait(false);
        PowerStatusChanged?.Invoke(this, newStatus);
    }
}
