// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

internal class Http3StreamContext : HttpConnectionContext
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
        IHttp3StreamLifetimeHandler streamLifetimeHandler,
        ConnectionContext streamContext,
        Http3PeerSettings clientPeerSettings,
        Http3PeerSettings serverPeerSettings) : base(connectionId, protocols, altSvcHeader, connectionContext, serviceContext, connectionFeatures, memoryPool, localEndPoint, remoteEndPoint)
    {
        StreamLifetimeHandler = streamLifetimeHandler;
        StreamContext = streamContext;
        ClientPeerSettings = clientPeerSettings;
        ServerPeerSettings = serverPeerSettings;
    }

    public IHttp3StreamLifetimeHandler StreamLifetimeHandler { get; }
    public ConnectionContext StreamContext { get; }
    public Http3PeerSettings ClientPeerSettings { get; }
    public Http3PeerSettings ServerPeerSettings { get; }
}
