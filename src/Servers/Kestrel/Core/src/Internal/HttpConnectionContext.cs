// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal class HttpConnectionContext
    {
        public HttpConnectionContext(
            string connectionId,
            HttpProtocols protocols,
            ConnectionContext connectionContext,
            ServiceContext serviceContext,
            IFeatureCollection connectionFeatures,
            MemoryPool<byte> memoryPool,
            IPEndPoint? localEndPoint,
            IPEndPoint? remoteEndPoint,
            IDuplexPipe transport)
        {
            ConnectionId = connectionId;
            Protocols = protocols;
            ConnectionContext = connectionContext;
            ServiceContext = serviceContext;
            ConnectionFeatures = connectionFeatures;
            MemoryPool = memoryPool;
            LocalEndPoint = localEndPoint;
            RemoteEndPoint = remoteEndPoint;
            Transport = transport;
        }

        public string ConnectionId { get; }
        public HttpProtocols Protocols { get; }
        public ConnectionContext ConnectionContext { get; }
        public ServiceContext ServiceContext { get; }
        public IFeatureCollection ConnectionFeatures { get; }
        public MemoryPool<byte> MemoryPool { get; }
        public IPEndPoint? LocalEndPoint { get; }
        public IPEndPoint? RemoteEndPoint { get; }
        public IDuplexPipe Transport { get; }

        public ITimeoutControl TimeoutControl { get; set; } = default!; // Always set by HttpConnection
        public ExecutionContext? InitialExecutionContext { get; set; }
    }
}
