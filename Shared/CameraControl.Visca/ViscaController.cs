namespace CameraControl.Visca;

// Copyright 2020 Jon Skeet. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

public sealed class ViscaController : IDisposable
{
    internal static TimeSpan DefaultTimeout { get; } = TimeSpan.FromSeconds(15);

    private static readonly ViscaPacket PowerOnPacket = ViscaPacket.FromBytesWithPreformatting(0x81, 0x01, 0x04, 0x00, 0x02, 0xff);
    private static readonly ViscaPacket PowerOffPacket = ViscaPacket.FromBytesWithPreformatting(0x81, 0x01, 0x04, 0x00, 0x03, 0xff);
    private static readonly ViscaPacket GetPowerStatusPacket = ViscaPacket.FromBytesWithPreformatting(0x81, 0x09, 0x04, 0x00, 0xff);
    private static readonly ViscaPacket GetZoomPacket = ViscaPacket.FromBytesWithPreformatting(0x81, 0x09, 0x04, 0x47, 0xff);
    private static readonly ViscaPacket GetPanTiltPacket = ViscaPacket.FromBytesWithPreformatting(0x81, 0x09, 0x06, 0x12, 0xff);        

    public const Byte MinSpeed = 0x01;
    public const Byte MaxPanSpeed = 0x18;
    public const Byte MaxTiltSpeed = 0x14;
    public const Byte MaxZoomSpeed = 0x7;

    public static ViscaController ForTcp(String host, Int32 port, TimeSpan? commandTimeout = null, ILogger? logger = null, TcpSendLock? sendLock = null)
    {
        var viscaClient = new TcpViscaClient(host, port, logger, sendLock);

        return new ViscaController(viscaClient, commandTimeout ?? DefaultTimeout, logger);
    }

    internal ViscaController(IViscaClient viscaClient, TimeSpan commandTimeout, ILogger? logger)
    {
        ViscaClient = viscaClient;
        CommandTimeout = commandTimeout;
        Logger = logger;
        Stopwatch = Stopwatch.StartNew();
    }

    public Boolean? Connected => ViscaClient.IsConnected();

    private IViscaClient ViscaClient { get; }

    private TimeSpan CommandTimeout { get; }

    private ILogger? Logger { get; }

    private Stopwatch Stopwatch { get; }

    public void Dispose()
    {
        ViscaClient.Dispose();
    }

    public Task Reconnect(CancellationToken cancellationToken, String? host = null, Int32? port = null)
    {
        return ViscaClient.Reconnect(cancellationToken, host, port);
    }

    // Power

    public async Task PowerCycle(CancellationToken cancellationToken = default)
    {
        await PowerOff(cancellationToken).ConfigureAwait(false);
        // On Minrray cameras, connections fail immediately after PowerOff, so we retry here for a bit.
        await RetryWithConstantBackoff(async token => { await PowerOn(token).ConfigureAwait(false); return 0; },
            attempts: 10, perOperationTimeout: TimeSpan.FromSeconds(1), delay: TimeSpan.FromSeconds(1));
        
        // Keep trying to get the power status until we can actually do so. This can take a few attempts.
        // Note that we don't actually try to validate that the power status is "on".
        var status = await RetryWithConstantBackoff(GetPowerStatus, attempts: 20, perOperationTimeout: TimeSpan.FromSeconds(1), delay: TimeSpan.FromSeconds(2));
        Logger?.LogDebug("Power cycle complete; power status: {status}", status);

        async Task<T> RetryWithConstantBackoff<T>(Func<CancellationToken, Task<T>> operation, int attempts, TimeSpan perOperationTimeout, TimeSpan delay)
        {
            int attemptsLeft = attempts;
            while (true)
            {
                try
                {
                    var shortCancellationToken = new CancellationTokenSource(perOperationTimeout).Token;
                    var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, shortCancellationToken).Token;
                    var result = await operation(linkedToken).ConfigureAwait(false);
                    return result;
                }
                catch (Exception) when (!cancellationToken.IsCancellationRequested && attemptsLeft > 0)
                {
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    attemptsLeft--;
                }
            }
        }
    }

    public async Task PowerOn(CancellationToken cancellationToken = default) =>
        await SendAsync(PowerOnPacket, cancellationToken).ConfigureAwait(false);

    public async Task PowerOff(CancellationToken cancellationToken = default) =>
        await SendAsync(PowerOffPacket, cancellationToken).ConfigureAwait(false);        

    public async Task<PowerStatus> GetPowerStatus(CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(GetPowerStatusPacket, cancellationToken).ConfigureAwait(false);
        return (PowerStatus) response.GetByte(2);
    }

    public async Task<PowerStatus> GetUpdatedPowerStatus(PowerStatus lastPowerStatus, CancellationToken cancellationToken = default)
    {
        const Int32 perOperationTimeout = 1;
        const Int32 perOperationDelay = 2;

        var attemptsLeft = 20;

        while (attemptsLeft > 0)
        {           
            try
            {
                var shortCancellationToken = new CancellationTokenSource(perOperationTimeout).Token;
                var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, shortCancellationToken).Token;
                var powerStatus = await GetPowerStatus(linkedToken).ConfigureAwait(false);

                if (powerStatus != PowerStatus.Unknown && powerStatus != lastPowerStatus)
                {
                    return powerStatus;
                }                
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(perOperationDelay, cancellationToken).ConfigureAwait(false);

                attemptsLeft--;
            }
        }

        return PowerStatus.Unknown;
    }

    // Main Commands

    public async Task MemorySet(byte slot, CancellationToken cancellationToken = default)
    {
        await SendAsync(new byte[] { 0x81, 0x01, 0x04, 0x3f, 0x01, slot, 0xff }, cancellationToken).ConfigureAwait(false);
    }

    public async Task MemoryRecall(byte slot, CancellationToken cancellationToken = default)
    {
        await SendAsync(new byte[] { 0x81, 0x01, 0x04, 0x3f, 0x02, slot, 0xff }, cancellationToken).ConfigureAwait(false);
    }

    public async Task GoHome(CancellationToken cancellationToken = default)
    {
        await SendAsync(new byte[] { 0x81, 0x01, 0x06, 0x04, 0xff }, cancellationToken).ConfigureAwait(false);
    }            

    /// <summary>
    /// Changes the current rate of pan/tilt.
    /// </summary>
    /// <param name="pan">true to pan right (positive); false to pan left (negative); null for no pan</param>
    /// <param name="tilt">true to tilt up (positive); false to tilt down (negative); null for no tilt</param>
    /// <param name="panSpeed">The speed at which to pan</param>
    /// <param name="tiltSpeed">The speed at which to tilt</param>
    /// <param name="cancellationToken">A cancellation token for the operation</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public async Task ContinuousPanTilt(bool? pan, bool? tilt, byte panSpeed, byte tiltSpeed, CancellationToken cancellationToken = default)
    {
        byte actualPan = (byte)(pan is null ? 3 : pan.Value ? 2 : 1);
        byte actualTilt = (byte)(tilt is null ? 3 : tilt.Value ? 1 : 2);
        byte[] bytes = new byte[] { 0x81, 0x01, 0x06, 0x1, panSpeed, tiltSpeed, actualPan, actualTilt, 0xff };
        await SendAsync(bytes, cancellationToken).ConfigureAwait(false);
    }

    public async Task ContinuousZoom(bool? zoomIn, byte zoomSpeed, CancellationToken cancellationToken = default)
    {
        byte parameter = (byte)(zoomIn is null ? 0 : zoomIn.Value ? 0x20 | zoomSpeed : 0x30 | zoomSpeed);
        await SendAsync(new byte[] { 0x81, 0x01, 0x04, 0x07, parameter, 0xff }, cancellationToken).ConfigureAwait(false);
    }

    // Other Commands

    public async Task<short> GetZoom(CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(GetZoomPacket, cancellationToken).ConfigureAwait(false);
        return response.GetInt16(2);
    }

    // TODO: What about 41, 42 etc? Would that be "direct with variable speed"?
    public async Task SetZoom(short zoom, CancellationToken cancellationToken = default)
    {
        byte[] bytes = new byte[] { 0x81, 0x01, 0x04, 0x47, 0, 0, 0, 0, 0xff };
        SetInt16(bytes, 4, zoom);
        await SendAsync(bytes, cancellationToken).ConfigureAwait(false);
    }

    public async Task<(short pan, short tilt)> GetPanTilt(CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(GetPanTiltPacket, cancellationToken).ConfigureAwait(false);
        return (response.GetInt16(2), response.GetInt16(6));
    }
  
    public async Task SetPanTilt(short pan, short tilt, byte panSpeed, byte tiltSpeed, CancellationToken cancellationToken = default)
    {
        byte[] bytes = new byte[] { 0x81, 0x01, 0x06, 0x02, panSpeed, tiltSpeed, 0, 0, 0, 0, 0, 0, 0, 0, 0xff };
        SetInt16(bytes, 6, pan);
        SetInt16(bytes, 10, tilt);
        await SendAsync(bytes, cancellationToken).ConfigureAwait(false);
    }

    public async Task RelativePanTilt(short pan, short tilt, byte panSpeed, byte tiltSpeed, CancellationToken cancellationToken = default)
    {
        byte[] bytes = new byte[] { 0x81, 0x01, 0x06, 0x03, panSpeed, tiltSpeed, 0, 0, 0, 0, 0, 0, 0, 0, 0xff };
        SetInt16(bytes, 6, pan);
        SetInt16(bytes, 10, tilt);
        await SendAsync(bytes, cancellationToken).ConfigureAwait(false);
    }               

    // Util

    private Task<ViscaPacket> SendAsync(byte[] packet, CancellationToken cancellationToken, [CallerMemberName] string? command = null)
    {
        var request = ViscaPacket.FromBytes(packet, 0, packet.Length);
        return SendAsync(request, cancellationToken, command);
    }

    private async Task<ViscaPacket> SendAsync(ViscaPacket packet, CancellationToken cancellationToken, [CallerMemberName] string? command = null)
    {
        var effectiveToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, new CancellationTokenSource(CommandTimeout).Token).Token;
        Logger?.LogDebug("Sending VISCA command '{command}': {request}", command, packet);
        long ticksBefore = Stopwatch.ElapsedTicks;
        var response = await ViscaClient.SendAsync(packet, effectiveToken).ConfigureAwait(false);
        long ticksAfter = Stopwatch.ElapsedTicks;
        Logger?.LogDebug("VISCA command '{command}' completed in {millis}ms", command, (ticksAfter - ticksBefore) * 1000 / Stopwatch.Frequency);
        return response;
    }

    private static void SetInt16(byte[] buffer, int start, short value)
    {
        buffer[start] = (byte)((value >> 12) & 0xf);
        buffer[start + 1] = (byte)((value >> 8) & 0xf);
        buffer[start + 2] = (byte)((value >> 4) & 0xf);
        buffer[start + 3] = (byte)((value >> 0) & 0xf);
    }
}
