// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting.Server;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal class HttpMultiplexedConnectionMiddleware<TContext> where TContext : notnull
    {
        private readonly ServiceContext _serviceContext;
        private readonly IHttpApplication<TContext> _application;

        public HttpMultiplexedConnectionMiddleware(ServiceContext serviceContext, IHttpApplication<TContext> application)
        {
            _serviceContext = serviceContext;
            _application = application;
        }

        public Task OnConnectionAsync(MultiplexedConnectionContext connectionContext)
        {
            var memoryPoolFeature = connectionContext.Features.Get<IMemoryPoolFeature>();

            var httpConnectionContext = new HttpMultiplexedConnectionContext(
                connectionContext.ConnectionId,
                connectionContext,
                _serviceContext,
                connectionContext.Features,
                memoryPoolFeature?.MemoryPool ?? System.Buffers.MemoryPool<byte>.Shared,
                connectionContext.LocalEndPoint as IPEndPoint,
                connectionContext.RemoteEndPoint as IPEndPoint);

            var connection = new HttpConnection(httpConnectionContext);

            return connection.ProcessRequestsAsync(_application);
        }
    }
}
