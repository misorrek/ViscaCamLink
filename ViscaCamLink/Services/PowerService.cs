namespace ViscaCamLink.Services;

using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using ViscaCamLink.Visca;
using ViscaCamLink.Visca.Types;

public partial class PowerService(IViscaController viscaController, ILogger<PowerService> logger) : IPowerService
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
        try
        {
            await SwitchPowerInternalAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // Logged here with context and rethrown; callers (TryCameraOperation) swallow without logging.
            LogSwitchPowerFailed(logger, exception);

            throw;
        }
    }

    private async Task SwitchPowerInternalAsync()
    {
        var currentStatus = await viscaController.GetPowerStatusAsync().ConfigureAwait(false);

        switch (currentStatus)
        {
            case PowerStatus.On:
                LogSwitchingPower(logger, currentStatus, PowerStatus.Standby);
                SwitchingPower?.Invoke(this, EventArgs.Empty);
                await viscaController.PowerOffAsync().ConfigureAwait(false);
                break;
            case PowerStatus.Standby:
                LogSwitchingPower(logger, currentStatus, PowerStatus.On);
                SwitchingPower?.Invoke(this, EventArgs.Empty);
                await viscaController.PowerOnAsync().ConfigureAwait(false);
                break;
            default:
                LogSwitchPowerSkipped(logger, currentStatus);
                return;
        }

        var newStatus = await viscaController.GetUpdatedPowerStatusAsync(currentStatus).ConfigureAwait(false);

        LogPowerSwitched(logger, newStatus);
        PowerStatusChanged?.Invoke(this, newStatus);
    }

    [LoggerMessage(EventId = 2101, Level = LogLevel.Information, Message = "Switching camera power from {CurrentStatus} to {TargetStatus}")]
    private static partial void LogSwitchingPower(ILogger logger, PowerStatus currentStatus, PowerStatus targetStatus);

    [LoggerMessage(EventId = 2102, Level = LogLevel.Information, Message = "Camera power status after switch: {Status}")]
    private static partial void LogPowerSwitched(ILogger logger, PowerStatus status);

    [LoggerMessage(EventId = 2103, Level = LogLevel.Warning, Message = "Power switch skipped because the current power status is {Status}")]
    private static partial void LogSwitchPowerSkipped(ILogger logger, PowerStatus status);

    [LoggerMessage(EventId = 2104, Level = LogLevel.Error, Message = "Switching camera power failed")]
    private static partial void LogSwitchPowerFailed(ILogger logger, Exception exception);
}
