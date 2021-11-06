// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Connections;

/// <summary>
/// Encapsulates all information about a multiplexed connection.
/// </summary>
public abstract class MultiplexedConnectionContext : BaseConnectionContext, IAsyncDisposable
{
    /// <summary>
    /// Asynchronously accept an incoming stream on the connection.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public abstract ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an outbound connection
    /// </summary>
    /// <param name="features"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public abstract ValueTask<ConnectionContext> ConnectAsync(IFeatureCollection? features = null, CancellationToken cancellationToken = default);
}
