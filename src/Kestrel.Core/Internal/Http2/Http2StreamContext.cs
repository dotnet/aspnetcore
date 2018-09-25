// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Net;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public class Http2StreamContext : IHttpProtocolContext
    {
        public string ConnectionId { get; set; }
        public int StreamId { get; set; }
        public ServiceContext ServiceContext { get; set; }
        public IFeatureCollection ConnectionFeatures { get; set; }
        public MemoryPool<byte> MemoryPool { get; set; }
        public IPEndPoint RemoteEndPoint { get; set; }
        public IPEndPoint LocalEndPoint { get; set; }
        public IHttp2StreamLifetimeHandler StreamLifetimeHandler { get; set; }
        public Http2PeerSettings ClientPeerSettings { get; set; }
        public Http2PeerSettings ServerPeerSettings { get; set; }
        public Http2FrameWriter FrameWriter { get; set; }
        public InputFlowControl ConnectionInputFlowControl { get; set; }
        public OutputFlowControl ConnectionOutputFlowControl { get; set; }
        public ITimeoutControl TimeoutControl { get; set; }
    }
}
