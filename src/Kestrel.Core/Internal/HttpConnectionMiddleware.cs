// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    public class HttpConnectionMiddleware<TContext>
    {
        private static long _lastHttpConnectionId = long.MinValue;

        private readonly IList<IConnectionAdapter> _connectionAdapters;
        private readonly ServiceContext _serviceContext;
        private readonly IHttpApplication<TContext> _application;
        private readonly HttpProtocols _protocols;

        public HttpConnectionMiddleware(IList<IConnectionAdapter> adapters, ServiceContext serviceContext, IHttpApplication<TContext> application, HttpProtocols protocols)
        {
            _serviceContext = serviceContext;
            _application = application;
            _protocols = protocols;

            // Keeping these around for now so progress can be made without updating tests
            _connectionAdapters = adapters;
        }

        public async Task OnConnectionAsync(ConnectionContext connectionContext)
        {
            // We need the transport feature so that we can cancel the output reader that the transport is using
            // This is a bit of a hack but it preserves the existing semantics
            var applicationFeature = connectionContext.Features.Get<IApplicationTransportFeature>();
            var memoryPoolFeature = connectionContext.Features.Get<IMemoryPoolFeature>();

            var httpConnectionId = Interlocked.Increment(ref _lastHttpConnectionId);

            var httpConnectionContext = new HttpConnectionContext
            {
                ConnectionId = connectionContext.ConnectionId,
                ConnectionContext = connectionContext,
                HttpConnectionId = httpConnectionId,
                Protocols = _protocols,
                ServiceContext = _serviceContext,
                ConnectionFeatures = connectionContext.Features,
                MemoryPool = memoryPoolFeature.MemoryPool,
                ConnectionAdapters = _connectionAdapters,
                Transport = connectionContext.Transport,
                Application = applicationFeature.Application
            };

            var connectionFeature = connectionContext.Features.Get<IHttpConnectionFeature>();
            var lifetimeFeature = connectionContext.Features.Get<IConnectionLifetimeFeature>();

            if (connectionFeature != null)
            {
                if (connectionFeature.LocalIpAddress != null)
                {
                    httpConnectionContext.LocalEndPoint = new IPEndPoint(connectionFeature.LocalIpAddress, connectionFeature.LocalPort);
                }

                if (connectionFeature.RemoteIpAddress != null)
                {
                    httpConnectionContext.RemoteEndPoint = new IPEndPoint(connectionFeature.RemoteIpAddress, connectionFeature.RemotePort);
                }
            }

            var connection = new HttpConnection(httpConnectionContext);
            _serviceContext.ConnectionManager.AddConnection(httpConnectionId, connection);

            try
            {
                var processingTask = connection.ProcessRequestsAsync(_application);

                connectionContext.Transport.Input.OnWriterCompleted(
                    (_, state) => ((HttpConnection)state).OnInputOrOutputCompleted(),
                    connection);

                connectionContext.Transport.Output.OnReaderCompleted(
                    (_, state) => ((HttpConnection)state).OnInputOrOutputCompleted(),
                    connection);

                await CancellationTokenAsTask(lifetimeFeature.ConnectionClosed);

                connection.OnConnectionClosed();

                await processingTask;
            }
            finally
            {
                _serviceContext.ConnectionManager.RemoveConnection(httpConnectionId);
            }
        }

        private static Task CancellationTokenAsTask(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }

            // Transports already dispatch prior to tripping ConnectionClosed
            // since application code can register to this token.
            var tcs = new TaskCompletionSource<object>();
            token.Register(() => tcs.SetResult(null));
            return tcs.Task;
        }
    }
}
