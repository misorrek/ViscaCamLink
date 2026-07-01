namespace ViscaCamLink.Visca;

using System.Diagnostics;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging;
using ViscaCamLink.Visca.Types;

public sealed partial class ViscaController(IViscaClient viscaClient, ILogger logger) : IViscaController
{
    private readonly Stopwatch performanceTimer = Stopwatch.StartNew();

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

    public bool? Connected => viscaClient.IsConnected();
    public byte MaxPanSpeed => ViscaProtocol.MaxPanSpeed;
    public byte MaxTiltSpeed => ViscaProtocol.MaxTiltSpeed;
    public byte MaxZoomSpeed => ViscaProtocol.MaxZoomSpeed;

    public void Dispose() => viscaClient.Dispose();

    public Task Reconnect(CancellationToken cancellationToken, string? host = null, int? port = null) =>
        viscaClient.Reconnect(cancellationToken, host, port);

    public async Task PowerOn(CancellationToken cancellationToken = default) =>
        await SendCommandAsync(PowerOnPacket, cancellationToken).ConfigureAwait(false);

    public async Task PowerOff(CancellationToken cancellationToken = default) =>
        await SendCommandAsync(PowerOffPacket, cancellationToken).ConfigureAwait(false);

    public async Task<PowerStatus> GetPowerStatus(CancellationToken cancellationToken = default)
    {
        var response = await SendCommandAsync(PowerStatusInquiryPacket, cancellationToken).ConfigureAwait(false);

        if (response.Length <= ViscaProtocol.PowerStatusByteIndex)
        {
            throw new ViscaProtocolException($"Power status response too short. Packet: {response}");
        }

        var rawStatus = response[ViscaProtocol.PowerStatusByteIndex];
        
        if (!Enum.IsDefined(typeof(PowerStatus), (int)rawStatus))
        {
            throw new ViscaProtocolException($"Unexpected power status value '{rawStatus}'. Packet: {response}");
        }

        return (PowerStatus)rawStatus;
    }

    public async Task<PowerStatus> GetUpdatedPowerStatus(PowerStatus lastPowerStatus, CancellationToken cancellationToken = default)
    {
        int attemptsRemaining = ViscaProtocol.PowerStatusPollAttempts;

        while (attemptsRemaining > 0)
        {
            try
            {
                using var perAttemptTimeout = new CancellationTokenSource(ViscaProtocol.PerOperationTimeoutMs);
                using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, perAttemptTimeout.Token);

                var currentStatus = await GetPowerStatus(linkedCancellation.Token).ConfigureAwait(false);

                if (currentStatus != PowerStatus.Unknown && currentStatus != lastPowerStatus)
                {
                    return currentStatus;
                }
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested) { }

            attemptsRemaining--;

            if (attemptsRemaining > 0)
            {
                await Task.Delay(ViscaProtocol.PerOperationDelayMs, cancellationToken).ConfigureAwait(false);
            }
        }

        return PowerStatus.Unknown;
    }

    public async Task MemorySet(byte slot, CancellationToken cancellationToken = default)
    {
        byte[] packet =
        [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryCamera,
            ViscaProtocol.CmdMemoryReset, ViscaProtocol.MemorySubSet, slot, ViscaProtocol.Terminator
        ];
        await SendCommandAsync(packet, cancellationToken).ConfigureAwait(false);
    }

    public async Task MemoryRecall(byte slot, CancellationToken cancellationToken = default)
    {
        byte[] packet =
        [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryCamera,
            ViscaProtocol.CmdMemoryReset, ViscaProtocol.MemorySubRecall, slot, ViscaProtocol.Terminator
        ];
        await SendCommandAsync(packet, cancellationToken).ConfigureAwait(false);
    }

    public async Task GoHome(CancellationToken cancellationToken = default) =>
        await SendCommandAsync(GoHomePacket, cancellationToken).ConfigureAwait(false);

    public async Task ContinuousPanTilt(PanTiltDirection panTiltDirection, byte panSpeed, byte tiltSpeed, CancellationToken cancellationToken = default)
    {
        var panTiltTuple = EncodePanTiltDirection(panTiltDirection);

        byte[] packet =
        [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryPanTilt,
            ViscaProtocol.CmdContinuousPanTilt, panSpeed, tiltSpeed, panTiltTuple.Item1, panTiltTuple.Item2, ViscaProtocol.Terminator
        ];
        await SendCommandAsync(packet, cancellationToken).ConfigureAwait(false);
    }

    public async Task ContinuousZoom(ZoomDirection zoomDirection, byte zoomSpeed, CancellationToken cancellationToken = default)
    {
        byte zoomParameter = EncodeZoomDirection(zoomDirection, zoomSpeed);

        byte[] packet =
        [
            ViscaProtocol.CameraAddress, ViscaProtocol.CommandPrefix, ViscaProtocol.CategoryCamera,
            ViscaProtocol.CmdZoomVariable, zoomParameter, ViscaProtocol.Terminator
        ];
        await SendCommandAsync(packet, cancellationToken).ConfigureAwait(false);
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

        var ticksBefore = performanceTimer.ElapsedTicks;
        var response = await viscaClient.SendAsync(packet, linkedCancellation.Token).ConfigureAwait(false);
        var ticksAfter = performanceTimer.ElapsedTicks;

        var elapsedMilliseconds = (ticksAfter - ticksBefore) * 1000 / Stopwatch.Frequency;
        LogCompletedCommand(logger, commandName, elapsedMilliseconds);

        return response;
    }

    private static Tuple<byte, byte> EncodePanTiltDirection(PanTiltDirection moveDirection)
    {
        if (moveDirection == PanTiltDirection.None)
        {
            return Tuple.Create(ViscaProtocol.DirectionStop, ViscaProtocol.DirectionStop);
        }

        bool? panLeft =
            moveDirection.HasFlag(PanTiltDirection.PanLeft) ? true :
            moveDirection.HasFlag(PanTiltDirection.PanRight) ? false :
            null;

        bool? tiltUp =
            moveDirection.HasFlag(PanTiltDirection.TiltUp) ? true :
            moveDirection.HasFlag(PanTiltDirection.TiltDown) ? false :
            null;

        return Tuple.Create(EncodeDirection(panLeft), EncodeDirection(tiltUp));
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

    [LoggerMessage(EventId = 1101, Level = LogLevel.Debug, Message = "Sending VISCA command '{Command}'")]
    private static partial void LogSendingCommand(ILogger logger, string? command);

    [LoggerMessage(EventId = 1102, Level = LogLevel.Debug, Message = "VISCA command '{Command}' completed in {Millis}ms")]
    private static partial void LogCompletedCommand(ILogger logger, string? command, long millis);

}
