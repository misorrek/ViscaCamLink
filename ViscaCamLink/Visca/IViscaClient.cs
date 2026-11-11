// Copyright 2020 Jon Skeet. All rights reserved.
// Adapted from jskeet/DemoCode (Apache-2.0); modified by Max Groiser.

namespace ViscaCamLink.Visca;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface IViscaClient : IDisposable
{
    bool? IsConnected();

    Task ReconnectAsync(string? host = null, int? port = null, CancellationToken cancellationToken = default);

    Task<ViscaPacket> SendAsync(ViscaPacket request, CancellationToken cancellationToken);
}
