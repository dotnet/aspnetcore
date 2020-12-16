// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal class Http3StreamContext : HttpConnectionContext
    {
        public Http3StreamContext(
            string connectionId,
            HttpProtocols protocols,
            ConnectionContext connectionContext,
            ServiceContext serviceContext,
            IFeatureCollection connectionFeatures,
            MemoryPool<byte> memoryPool,
            IPEndPoint? localEndPoint,
            IPEndPoint? remoteEndPoint,
            IDuplexPipe transport,
            ConnectionContext streamContext) : base(connectionId, protocols, connectionContext, serviceContext, connectionFeatures, memoryPool, localEndPoint, remoteEndPoint, transport)
        {
            StreamContext = streamContext;
        }

        public ConnectionContext StreamContext { get; }
    }
}
