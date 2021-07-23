// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal class HttpMultiplexedConnectionContext : BaseHttpConnectionContext
    {
        public HttpMultiplexedConnectionContext(
            string connectionId,
            MultiplexedConnectionContext connectionContext,
            ServiceContext serviceContext,
            IFeatureCollection connectionFeatures,
            MemoryPool<byte> memoryPool,
            IPEndPoint? localEndPoint,
            IPEndPoint? remoteEndPoint) : base(connectionId, HttpProtocols.Http3, connectionContext, serviceContext, connectionFeatures, memoryPool, localEndPoint, remoteEndPoint)
        {
        }
    }
}
