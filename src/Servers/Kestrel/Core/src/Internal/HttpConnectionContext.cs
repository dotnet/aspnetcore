// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal class HttpConnectionContext : BaseHttpConnectionContext
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
            IDuplexPipe transport) : base(connectionId, protocols, connectionContext, serviceContext, connectionFeatures, memoryPool, localEndPoint, remoteEndPoint)
        {
            Transport = transport;
        }

        public IDuplexPipe Transport { get; }
    }
}
