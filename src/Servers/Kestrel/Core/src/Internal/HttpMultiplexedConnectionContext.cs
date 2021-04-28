// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
