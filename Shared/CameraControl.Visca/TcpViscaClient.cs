namespace CameraControl.Visca
{
    // Copyright 2020 Jon Skeet. All rights reserved.
    // Use of this source code is governed by the Apache License 2.0,
    // as found in the LICENSE.txt file.

    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    internal sealed class TcpViscaClient : ViscaClientBase
    {
        public TcpViscaClient(string host, int port, ILogger? logger, TcpSendLock? sendLock) : base(logger)
        {
            Host = host;
            Port = port;
            SendLock = sendLock ?? new TcpSendLock(null);
        }            

        public String Host { get; private set; }

        public Int32 Port { get; private set; }

        private TcpSendLock SendLock { get; }

        private Byte[] WriteBuffer { get; } = new Byte[16];

        private ReadBuffer ReadBuffer { get; } = new ReadBuffer();

        private Boolean IsFirstConnection { get; set; } = true;

        private TcpClient? TcpClient { get; set; }

        private Stream? Stream { get; set; }                

        public override void Dispose()
        {
            TcpClient?.Dispose();
        }

        public override Boolean? IsConnected()
        {
            return TcpClient?.Connected;
        }
        public override Task Reconnect(CancellationToken cancellationToken, String? host = null, Int32? port = null)
        {
            Host = host ?? Host;
            Port = port ?? Port;

            return ReconnectAsync(cancellationToken);
        }
        
        protected override void Disconnect()
        {
            ReadBuffer.Clear();
            TcpClient?.Dispose();            
            TcpClient = null; // This is the trigger for reconnection next time.
        }

        protected override async Task ReconnectAsync(CancellationToken cancellationToken)
        {
            if (IsFirstConnection)
            {
                Logger?.LogDebug("Connecting to {host}:{port}", Host, Port);
                IsFirstConnection = false;
            }
            else
            {
                Logger?.LogDebug("Reconnecting to {host}:{port}", Host, Port);
            }
            ReadBuffer.Clear();
            TcpClient?.Dispose();
            TcpClient = new TcpClient { NoDelay = true };            

            try
            {
                await TcpClient.ConnectAsync(Host, Port, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                TcpClient = null;
                throw;
            }
            Stream = TcpClient.GetStream();
        }

        protected override async Task SendPacketAsync(ViscaPacket packet, CancellationToken cancellationToken)
        {
            if (TcpClient is null)
            {
                await ReconnectAsync(cancellationToken).ConfigureAwait(false);
            }
            for (int i = 0; i < packet.Length; i++)
            {
                WriteBuffer[i] = packet.GetByte(i);
            }
            await SendLock.AcquireAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // stream is never null if either client wasn't null, or after reconnection.
                await Stream!.WriteAsync(WriteBuffer.AsMemory(0, packet.Length), cancellationToken).ConfigureAwait(false);

                await SendLock.PostSendDelayAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                SendLock.Release();
            }
        }

        protected override Task<ViscaPacket> ReceivePacketAsync(CancellationToken cancellationToken)
        {
            if (Stream is null)
            {
                throw new ViscaProtocolException("Cannot receive a packet before sending one");
            }
            return ReadBuffer.ReadAsync(Stream, cancellationToken);
        }
    }
}
