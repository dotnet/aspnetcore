// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal sealed class Http2StreamContext : HttpConnectionContext
    {
        public Http2StreamContext(
            string connectionId,
            HttpProtocols protocols,
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
            OutputFlowControl connectionOutputFlowControl) : base(connectionId, protocols, connectionContext: null!, serviceContext, connectionFeatures, memoryPool, localEndPoint, remoteEndPoint)
        {
            StreamId = streamId;
            StreamLifetimeHandler = streamLifetimeHandler;
            ClientPeerSettings = clientPeerSettings;
            ServerPeerSettings = serverPeerSettings;
            FrameWriter = frameWriter;
            ConnectionInputFlowControl = connectionInputFlowControl;
            ConnectionOutputFlowControl = connectionOutputFlowControl;
        }

        public IHttp2StreamLifetimeHandler StreamLifetimeHandler { get; }
        public Http2PeerSettings ClientPeerSettings { get; }
        public Http2PeerSettings ServerPeerSettings { get; }
        public Http2FrameWriter FrameWriter { get; }
        public InputFlowControl ConnectionInputFlowControl { get; }
        public OutputFlowControl ConnectionOutputFlowControl { get; }

        public int StreamId { get; set; }
    }
}
