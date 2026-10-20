namespace ViscaCamLink.Visca;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using ViscaCamLink.Visca.Types;

public partial class ViscaController(IViscaClient viscaClient, ILogger<ViscaController> logger) : IViscaController
{
    private static readonly ViscaPacket PowerOnPacket = ViscaPacket.FromBytesWithPreformatting(
        ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryCamera,
        ViscaProtocol.CmdPower, ViscaProtocol.PowerOnArg, ViscaProtocol.Terminator);

    private static readonly ViscaPacket PowerOffPacket = ViscaPacket.FromBytesWithPreformatting(
        ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryCamera,
        ViscaProtocol.CmdPower, ViscaProtocol.PowerOffArg, ViscaProtocol.Terminator);

    private static readonly ViscaPacket PowerStatusInquiryPacket = ViscaPacket.FromBytesWithPreformatting(
        ViscaProtocol.CameraAddress, ViscaProtocol.InquiryPrefix, ViscaProtocol.CategoryCamera,
        ViscaProtocol.CmdPower, ViscaProtocol.Terminator);

    private static readonly ViscaPacket GoHomePacket = ViscaPacket.FromBytesWithPreformatting(
        ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryPanTilt,
        ViscaProtocol.CmdHome, ViscaProtocol.Terminator);

    private static readonly ViscaPacket OnePushFocusPacket = ViscaPacket.FromBytesWithPreformatting(
        ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryCamera,
        ViscaProtocol.CmdFocusOnePush, ViscaProtocol.FocusOnePushTrigger, ViscaProtocol.Terminator);

    private readonly Stopwatch _performanceTimer = Stopwatch.StartNew();

    public bool? Connected => viscaClient.IsConnected();

    public byte MaxPanSpeed => ViscaProtocol.MaxPanSpeed;

    public byte MaxTiltSpeed => ViscaProtocol.MaxTiltSpeed;

    public byte MaxZoomSpeed => ViscaProtocol.MaxZoomSpeed;

    public byte MaxFocusSpeed => ViscaProtocol.MaxFocusSpeed;

    public void Dispose() => viscaClient.Dispose();

    public Task ReconnectAsync(string? host = null, int? port = null, CancellationToken cancellationToken = default) =>
        viscaClient.ReconnectAsync(host, port, cancellationToken);

    public async Task PowerOnAsync(CancellationToken cancellationToken = default) =>
        await SendCommandAsync(PowerOnPacket, cancellationToken).ConfigureAwait(false);

    public async Task PowerOffAsync(CancellationToken cancellationToken = default) =>
        await SendCommandAsync(PowerOffPacket, cancellationToken).ConfigureAwait(false);

    public async Task<PowerStatus> GetPowerStatusAsync(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync(PowerStatusInquiryPacket, cancellationToken).ConfigureAwait(false);

        if (response.Length <= ViscaProtocol.PowerStatusByteIndex)
        {
            throw new ViscaProtocolException($"Power status response too short. Packet: {response}");
        }

        var rawStatus = response[ViscaProtocol.PowerStatusByteIndex];

        if (!Enum.IsDefined((PowerStatus)rawStatus))
        {
            throw new ViscaProtocolException($"Unexpected power status value '{rawStatus}'. Packet: {response}");
        }

        return (PowerStatus)rawStatus;
    }

    public async Task<PowerStatus> GetUpdatedPowerStatusAsync(PowerStatus lastPowerStatus, CancellationToken cancellationToken = default)
    {
        var attemptsRemaining = ViscaProtocol.PowerStatusPollAttempts;

        while (attemptsRemaining > 0)
        {
            try
            {
                using var pollTimeout = new CancellationTokenSource(ViscaProtocol.PowerStatusPollTimeout);
                using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, pollTimeout.Token);

                var currentStatus = await GetPowerStatusAsync(linkedCancellation.Token).ConfigureAwait(false);

                if (currentStatus != PowerStatus.Unknown && currentStatus != lastPowerStatus)
                {
                    return currentStatus;
                }
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                // A failed poll attempt is expected while the camera is switching power; keep polling.
            }

            attemptsRemaining--;

            if (attemptsRemaining > 0)
            {
                await Task.Delay(ViscaProtocol.PowerStatusPollDelay, cancellationToken).ConfigureAwait(false);
            }
        }

        return PowerStatus.Unknown;
    }

    public async Task MemorySetAsync(byte slot, CancellationToken cancellationToken = default)
    {
        byte[] packet =
        [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryCamera,
            ViscaProtocol.CmdMemoryReset, ViscaProtocol.MemorySubSet, slot, ViscaProtocol.Terminator
        ];

        await SendCommandAsync(packet, cancellationToken).ConfigureAwait(false);
    }

    public async Task MemoryRecallAsync(byte slot, CancellationToken cancellationToken = default)
    {
        byte[] packet =
        [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryCamera,
            ViscaProtocol.CmdMemoryReset, ViscaProtocol.MemorySubRecall, slot, ViscaProtocol.Terminator
        ];

        await SendCommandAsync(packet, cancellationToken).ConfigureAwait(false);
    }

    public async Task GoHomeAsync(CancellationToken cancellationToken = default) =>
        await SendCommandAsync(GoHomePacket, cancellationToken).ConfigureAwait(false);

    public async Task ContinuousPanTiltAsync(PanTiltDirection panTiltDirection, byte panSpeed, byte tiltSpeed, CancellationToken cancellationToken = default)
    {
        var (panDirection, tiltDirection) = EncodePanTiltDirection(panTiltDirection);

        byte[] packet =
        [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryPanTilt,
            ViscaProtocol.CmdContinuousPanTilt, panSpeed, tiltSpeed, panDirection, tiltDirection, ViscaProtocol.Terminator
        ];

        await SendCommandAsync(packet, cancellationToken).ConfigureAwait(false);
    }

    public async Task ContinuousZoomAsync(ZoomDirection zoomDirection, byte zoomSpeed, CancellationToken cancellationToken = default)
    {
        var zoomParameter = EncodeZoomDirection(zoomDirection, zoomSpeed);

        byte[] packet =
        [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryCamera,
            ViscaProtocol.CmdZoomVariable, zoomParameter, ViscaProtocol.Terminator
        ];

        await SendCommandAsync(packet, cancellationToken).ConfigureAwait(false);
    }

    public async Task ContinuousFocusAsync(FocusDirection focusDirection, byte focusSpeed, CancellationToken cancellationToken = default)
    {
        var speed = Math.Clamp(focusSpeed, ViscaProtocol.MinFocusSpeed, ViscaProtocol.MaxFocusSpeed);

        var focusParameter = focusDirection switch
        {
            FocusDirection.None => ViscaProtocol.FocusStop,
            FocusDirection.Far => (byte)(ViscaProtocol.FocusFarMask | speed),
            FocusDirection.Near => (byte)(ViscaProtocol.FocusNearMask | speed),
            _ => throw new ArgumentOutOfRangeException(nameof(focusDirection), focusDirection, null)
        };

        await SendCameraCommandAsync(ViscaProtocol.CmdFocus, focusParameter, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetFocusModeAsync(FocusMode focusMode, CancellationToken cancellationToken = default)
    {
        var modeParameter = focusMode switch
        {
            FocusMode.Auto => ViscaProtocol.FocusModeAuto,
            FocusMode.Manual => ViscaProtocol.FocusModeManual,
            FocusMode.Toggle => ViscaProtocol.FocusModeToggle,
            _ => throw new ArgumentOutOfRangeException(nameof(focusMode), focusMode, null)
        };

        await SendCameraCommandAsync(ViscaProtocol.CmdFocusMode, modeParameter, cancellationToken).ConfigureAwait(false);
    }

    public async Task TriggerOnePushFocusAsync(CancellationToken cancellationToken = default) =>
        await SendCommandAsync(OnePushFocusPacket, cancellationToken).ConfigureAwait(false);

    public async Task SetFocusPositionAsync(ushort focusPosition, CancellationToken cancellationToken = default)
    {
        byte[] packet =
        [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryCamera,
            ViscaProtocol.CmdFocusDirect,
            GetNibble(focusPosition, 12), GetNibble(focusPosition, 8), GetNibble(focusPosition, 4), GetNibble(focusPosition, 0),
            ViscaProtocol.Terminator
        ];

        await SendCommandAsync(packet, cancellationToken).ConfigureAwait(false);
    }

    public async Task AdjustIrisAsync(IrisDirection irisDirection, CancellationToken cancellationToken = default)
    {
        var irisParameter = irisDirection switch
        {
            IrisDirection.Reset => ViscaProtocol.IrisReset,
            IrisDirection.Up => ViscaProtocol.IrisUp,
            IrisDirection.Down => ViscaProtocol.IrisDown,
            _ => throw new ArgumentOutOfRangeException(nameof(irisDirection), irisDirection, null)
        };

        await SendCameraCommandAsync(ViscaProtocol.CmdIris, irisParameter, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetIrisPositionAsync(byte irisPosition, CancellationToken cancellationToken = default)
    {
        byte[] packet =
        [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryCamera,
            ViscaProtocol.CmdIrisDirect, ViscaProtocol.DirectPadding, ViscaProtocol.DirectPadding,
            GetNibble(irisPosition, 4), GetNibble(irisPosition, 0),
            ViscaProtocol.Terminator
        ];

        await SendCommandAsync(packet, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetExposureModeAsync(ExposureMode exposureMode, CancellationToken cancellationToken = default)
    {
        var modeParameter = exposureMode switch
        {
            ExposureMode.FullAuto => ViscaProtocol.ExposureModeFullAuto,
            ExposureMode.Manual => ViscaProtocol.ExposureModeManual,
            ExposureMode.ShutterPriority => ViscaProtocol.ExposureModeShutterPriority,
            ExposureMode.IrisPriority => ViscaProtocol.ExposureModeIrisPriority,
            ExposureMode.Bright => ViscaProtocol.ExposureModeBright,
            _ => throw new ArgumentOutOfRangeException(nameof(exposureMode), exposureMode, null)
        };

        await SendCameraCommandAsync(ViscaProtocol.CmdExposureMode, modeParameter, cancellationToken).ConfigureAwait(false);
    }

    private Task<ViscaPacket> SendCameraCommandAsync(byte command, byte parameter, CancellationToken cancellationToken, [CallerMemberName] string? commandName = null)
    {
        byte[] packet =
        [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryCamera,
            command, parameter, ViscaProtocol.Terminator
        ];

        return SendCommandAsync(packet, cancellationToken, commandName);
    }

    private Task<ViscaPacket> SendCommandAsync(byte[] packetBytes, CancellationToken cancellationToken, [CallerMemberName] string? commandName = null)
    {
        var packet = ViscaPacket.FromBytes(packetBytes, 0, packetBytes.Length);

        return SendCommandAsync(packet, cancellationToken, commandName);
    }

    private async Task<ViscaPacket> SendCommandAsync(ViscaPacket packet, CancellationToken cancellationToken, [CallerMemberName] string? commandName = null)
    {
        using var timeoutSource = new CancellationTokenSource(ViscaProtocol.DefaultCommandTimeout);
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

        LogSendingCommand(logger, commandName);

        var ticksBefore = _performanceTimer.ElapsedTicks;
        ViscaPacket response;

        try
        {
            response = await viscaClient.SendAsync(packet, linkedCancellation.Token).ConfigureAwait(false);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogCommandFailed(logger, commandName, exception);

            throw;
        }

        var ticksAfter = _performanceTimer.ElapsedTicks;
        var elapsedMilliseconds = (ticksAfter - ticksBefore) * 1000 / Stopwatch.Frequency;

        LogCompletedCommand(logger, commandName, elapsedMilliseconds);

        return response;
    }

    private static (byte Pan, byte Tilt) EncodePanTiltDirection(PanTiltDirection moveDirection)
    {
        if (moveDirection == PanTiltDirection.None)
        {
            return (ViscaProtocol.DirectionStop, ViscaProtocol.DirectionStop);
        }

        var panLeft =
            moveDirection.HasFlag(PanTiltDirection.PanLeft) ? true :
            moveDirection.HasFlag(PanTiltDirection.PanRight) ? false :
            (bool?)null;

        var tiltUp =
            moveDirection.HasFlag(PanTiltDirection.TiltUp) ? true :
            moveDirection.HasFlag(PanTiltDirection.TiltDown) ? false :
            (bool?)null;

        return (EncodeDirection(panLeft), EncodeDirection(tiltUp));
    }

    private static byte EncodeDirection(bool? directionUpOrLeft)
    {
        return directionUpOrLeft switch
        {
            null => ViscaProtocol.DirectionStop,
            true => ViscaProtocol.DirectionNegative,
            false => ViscaProtocol.DirectionPositive
        };
    }

    private static byte EncodeZoomDirection(ZoomDirection zoomDirection, byte speed)
    {
        return zoomDirection switch
        {
            ZoomDirection.None => ViscaProtocol.ZoomStop,
            ZoomDirection.In => (byte)(ViscaProtocol.ZoomInMask | speed),
            ZoomDirection.Out => (byte)(ViscaProtocol.ZoomOutMask | speed),
            _ => ViscaProtocol.ZoomStop
        };
    }

    private static byte GetNibble(int value, int shift) => (byte)((value >> shift) & ViscaProtocol.NibbleMask);

    [LoggerMessage(EventId = 1101, Level = LogLevel.Debug, Message = "Sending VISCA command '{Command}'")]
    private static partial void LogSendingCommand(ILogger logger, string? command);

    [LoggerMessage(EventId = 1102, Level = LogLevel.Debug, Message = "VISCA command '{Command}' completed in {Millis}ms")]
    private static partial void LogCompletedCommand(ILogger logger, string? command, long millis);

    [LoggerMessage(EventId = 1103, Level = LogLevel.Debug, Message = "VISCA command '{Command}' failed")]
    private static partial void LogCommandFailed(ILogger logger, string? command, Exception exception);
}
