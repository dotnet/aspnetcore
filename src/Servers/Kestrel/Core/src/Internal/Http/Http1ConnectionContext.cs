// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public class Http1ConnectionContext : IHttpProtocolContext
    {
        public string ConnectionId { get; set; }
        public ServiceContext ServiceContext { get; set; }
        public ConnectionContext ConnectionContext { get; set; }
        public IFeatureCollection ConnectionFeatures { get; set; }
        public MemoryPool<byte> MemoryPool { get; set; }
        public IPEndPoint RemoteEndPoint { get; set; }
        public IPEndPoint LocalEndPoint { get; set; }
        public ITimeoutControl TimeoutControl { get; set; }
        public IDuplexPipe Transport { get; set; }
        public IDuplexPipe Application { get; set; }
    }
}
