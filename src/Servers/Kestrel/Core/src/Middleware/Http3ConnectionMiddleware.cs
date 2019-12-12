// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting.Server;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal class Http3ConnectionMiddleware<TContext>
    {
        private readonly ServiceContext _serviceContext;
        private readonly IHttpApplication<TContext> _application;

        public Http3ConnectionMiddleware(ServiceContext serviceContext, IHttpApplication<TContext> application)
        {
            _serviceContext = serviceContext;
            _application = application;
        }

        public Task OnConnectionAsync(MultiplexedConnectionContext connectionContext)
        {
            var memoryPoolFeature = connectionContext.Features.Get<IMemoryPoolFeature>();

            var http3ConnectionContext = new Http3ConnectionContext
            {
                ConnectionId = connectionContext.ConnectionId,
                MultiplexedConnectionContext = connectionContext,
                ServiceContext = _serviceContext,
                ConnectionFeatures = connectionContext.Features,
                MemoryPool = memoryPoolFeature.MemoryPool,
                LocalEndPoint = connectionContext.LocalEndPoint as IPEndPoint,
                RemoteEndPoint = connectionContext.RemoteEndPoint as IPEndPoint
            };

            var connection = new Http3ConnectionTemp(http3ConnectionContext);

            return connection.ProcessRequestsAsync(_application);
        }
    }
}
