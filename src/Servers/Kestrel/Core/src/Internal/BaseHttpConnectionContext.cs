// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal;

internal class BaseHttpConnectionContext
{
    public BaseHttpConnectionContext(
        string connectionId,
        HttpProtocols protocols,
        AltSvcHeader? altSvcHeader,
        BaseConnectionContext connectionContext,
        ServiceContext serviceContext,
        IFeatureCollection connectionFeatures,
        MemoryPool<byte> memoryPool,
        IPEndPoint? localEndPoint,
        IPEndPoint? remoteEndPoint,
        ConnectionMetricsContext metricsContext)
    {
        ConnectionId = connectionId;
        Protocols = protocols;
        AltSvcHeader = altSvcHeader;
        ConnectionContext = connectionContext;
        ServiceContext = serviceContext;
        ConnectionFeatures = connectionFeatures;
        MemoryPool = memoryPool;
        LocalEndPoint = localEndPoint;
        RemoteEndPoint = remoteEndPoint;
        MetricsContext = metricsContext;
    }

    public string ConnectionId { get; set; }
    public HttpProtocols Protocols { get; }
    public AltSvcHeader? AltSvcHeader { get; }
    public BaseConnectionContext ConnectionContext { get; }
    public ServiceContext ServiceContext { get; }
    public IFeatureCollection ConnectionFeatures { get; }
    public MemoryPool<byte> MemoryPool { get; }
    public IPEndPoint? LocalEndPoint { get; }
    public IPEndPoint? RemoteEndPoint { get; }
    public ConnectionMetricsContext MetricsContext { get; }

    public ITimeoutControl TimeoutControl { get; set; } = default!; // Always set by HttpConnection
    public ExecutionContext? InitialExecutionContext { get; set; }
}
