// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal sealed class HttpMultiplexedConnectionContext : BaseHttpConnectionContext
{
    public HttpMultiplexedConnectionContext(
        string connectionId,
        HttpProtocols protocols,
        AltSvcHeader? altSvcHeader,
        MultiplexedConnectionContext connectionContext,
        ServiceContext serviceContext,
        IFeatureCollection connectionFeatures,
        MemoryPool<byte> memoryPool,
        IPEndPoint? localEndPoint,
        IPEndPoint? remoteEndPoint,
        ConnectionMetricsContext metricsContext) : base(connectionId, protocols, altSvcHeader, connectionContext, serviceContext, connectionFeatures, memoryPool, localEndPoint, remoteEndPoint, metricsContext)
    {
    }
}
