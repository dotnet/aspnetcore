// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Experimental;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal class Http3ConnectionContext
    {
        public Http3ConnectionContext(
            string connectionId,
            MultiplexedConnectionContext connectionContext,
            ServiceContext serviceContext,
            IFeatureCollection connectionFeatures,
            MemoryPool<byte> memoryPool,
            IPEndPoint? localEndPoint,
            IPEndPoint? remoteEndPoint)
        {
            ConnectionId = connectionId;
            ConnectionContext = connectionContext;
            ServiceContext = serviceContext;
            ConnectionFeatures = connectionFeatures;
            MemoryPool = memoryPool;
            LocalEndPoint = localEndPoint;
            RemoteEndPoint = remoteEndPoint;
        }

        public string ConnectionId { get; }
        public MultiplexedConnectionContext ConnectionContext { get; }
        public ServiceContext ServiceContext { get; }
        public IFeatureCollection ConnectionFeatures { get; }
        public MemoryPool<byte> MemoryPool { get; }
        public IPEndPoint? LocalEndPoint { get; }
        public IPEndPoint? RemoteEndPoint { get; }

        public ITimeoutControl TimeoutControl { get; set; } = default!; // Always set by HttpConnection
    }
}
