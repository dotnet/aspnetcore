// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.WebTransport;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

internal sealed class Http3StreamContext : HttpConnectionContext
{
    public Http3StreamContext(
        string connectionId,
        HttpProtocols protocols,
        AltSvcHeader? altSvcHeader,
        BaseConnectionContext connectionContext,
        ServiceContext serviceContext,
        IFeatureCollection connectionFeatures,
        MemoryPool<byte> memoryPool,
        IPEndPoint? localEndPoint,
        IPEndPoint? remoteEndPoint,
        ConnectionContext streamContext,
        Http3Connection connection) : base(connectionId, protocols, altSvcHeader, connectionContext, serviceContext, connectionFeatures, memoryPool, localEndPoint, remoteEndPoint, connection.MetricsContext)
    {
        StreamLifetimeHandler = connection._streamLifetimeHandler;
        StreamContext = streamContext;
        ClientPeerSettings = connection._clientSettings;
        ServerPeerSettings = connection._serverSettings;
        Connection = connection;
    }

    public IHttp3StreamLifetimeHandler StreamLifetimeHandler { get; }
    public ConnectionContext StreamContext { get; }
    public Http3PeerSettings ClientPeerSettings { get; }
    public Http3PeerSettings ServerPeerSettings { get; }
    public WebTransportSession? WebTransportSession { get; set; }
    public Http3Connection Connection { get; }
}
