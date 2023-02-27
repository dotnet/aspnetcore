// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Net;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal interface IMultiplexedTransportManager
{
    bool HasFactories { get; }

    Task<EndPoint> BindAsync(EndPoint endPoint, MultiplexedConnectionDelegate multiplexedConnectionDelegate, ListenOptions listenOptions, CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
    Task StopEndpointsAsync(List<EndpointConfig> endpointsToStop, CancellationToken cancellationToken);
}
