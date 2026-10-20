namespace ViscaCamLink.Visca;

using System;
using System.Threading;
using System.Threading.Tasks;

using ViscaCamLink.Visca.Types;

public interface IViscaController : IDisposable
{
    bool? Connected { get; }

    byte MaxPanSpeed { get; }

    byte MaxTiltSpeed { get; }

    byte MaxZoomSpeed { get; }

    byte MaxFocusSpeed { get; }

    Task ReconnectAsync(string? host = null, int? port = null, CancellationToken cancellationToken = default);

    Task PowerOnAsync(CancellationToken cancellationToken = default);

    Task PowerOffAsync(CancellationToken cancellationToken = default);

    Task<PowerStatus> GetPowerStatusAsync(CancellationToken cancellationToken = default);

    Task<PowerStatus> GetUpdatedPowerStatusAsync(PowerStatus lastPowerStatus, CancellationToken cancellationToken = default);

    Task MemorySetAsync(byte slot, CancellationToken cancellationToken = default);

    Task MemoryRecallAsync(byte slot, CancellationToken cancellationToken = default);

    Task GoHomeAsync(CancellationToken cancellationToken = default);

    Task ContinuousPanTiltAsync(PanTiltDirection panTiltDirection, byte panSpeed, byte tiltSpeed, CancellationToken cancellationToken = default);

    Task ContinuousZoomAsync(ZoomDirection zoomDirection, byte zoomSpeed, CancellationToken cancellationToken = default);

    /// <remarks>The speed is clamped to 0..<see cref="MaxFocusSpeed"/>. Only takes effect in manual focus mode.</remarks>
    Task ContinuousFocusAsync(FocusDirection focusDirection, byte focusSpeed, CancellationToken cancellationToken = default);

    Task SetFocusModeAsync(FocusMode focusMode, CancellationToken cancellationToken = default);

    Task TriggerOnePushFocusAsync(CancellationToken cancellationToken = default);

    /// <remarks>Only takes effect in manual focus mode; the valid range is camera-specific.</remarks>
    Task SetFocusPositionAsync(ushort focusPosition, CancellationToken cancellationToken = default);

    /// <remarks>
    /// Iris commands only take effect while the exposure mode is <see cref="ExposureMode.Manual"/> or
    /// <see cref="ExposureMode.IrisPriority"/>. The mode is intentionally not switched automatically;
    /// callers must call <see cref="SetExposureModeAsync"/> first.
    /// </remarks>
    Task AdjustIrisAsync(IrisDirection irisDirection, CancellationToken cancellationToken = default);

    /// <remarks>
    /// Only takes effect while the exposure mode is <see cref="ExposureMode.Manual"/> or <see cref="ExposureMode.IrisPriority"/>;
    /// the mode is not switched automatically. The valid range is camera-specific.
    /// </remarks>
    Task SetIrisPositionAsync(byte irisPosition, CancellationToken cancellationToken = default);

    Task SetExposureModeAsync(ExposureMode exposureMode, CancellationToken cancellationToken = default);
}
