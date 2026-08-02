// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

internal sealed class Http2StreamContext : HttpConnectionContext
{
    public Http2StreamContext(
        string connectionId,
        HttpProtocols protocols,
        AltSvcHeader? altSvcHeader,
        BaseConnectionContext connectionContext,
        ServiceContext serviceContext,
        IFeatureCollection connectionFeatures,
        MemoryPool<byte> memoryPool,
        IPEndPoint? localEndPoint,
        IPEndPoint? remoteEndPoint,
        int streamId,
        IHttp2StreamLifetimeHandler streamLifetimeHandler,
        Http2PeerSettings clientPeerSettings,
        Http2PeerSettings serverPeerSettings,
        Http2FrameWriter frameWriter,
        InputFlowControl connectionInputFlowControl,
        ConnectionMetricsContext metricsContext) : base(connectionId, protocols, altSvcHeader, connectionContext, serviceContext, connectionFeatures, memoryPool, localEndPoint, remoteEndPoint, metricsContext)
    {
        StreamId = streamId;
        StreamLifetimeHandler = streamLifetimeHandler;
        ClientPeerSettings = clientPeerSettings;
        ServerPeerSettings = serverPeerSettings;
        FrameWriter = frameWriter;
        ConnectionInputFlowControl = connectionInputFlowControl;
    }

    public IHttp2StreamLifetimeHandler StreamLifetimeHandler { get; }
    public Http2PeerSettings ClientPeerSettings { get; }
    public Http2PeerSettings ServerPeerSettings { get; }
    public Http2FrameWriter FrameWriter { get; }
    public InputFlowControl ConnectionInputFlowControl { get; }
    public int StreamId { get; set; }
}
