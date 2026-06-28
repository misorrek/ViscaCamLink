namespace ViscaCamLink.Services;

using ViscaCamLink.Visca.Types;

public interface IPowerService
{
    event EventHandler<PowerStatus>? PowerStatusChanged;
    event EventHandler? SwitchingPower;
    Task RefreshPowerStatusAsync();
    Task SwitchPowerAsync();
}
