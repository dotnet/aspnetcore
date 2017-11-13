using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Protocols;
using Microsoft.AspNetCore.Protocols.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    public class HttpConnectionMiddleware<TContext>
    {
        private static Action<Exception, object> _completeTcs = CompleteTcs;

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

        public Task OnConnectionAsync(ConnectionContext connectionContext)
        {
            // We need the transport feature so that we can cancel the output reader that the transport is using
            // This is a bit of a hack but it preserves the existing semantics
            var transportFeature = connectionContext.Features.Get<IConnectionTransportFeature>();

            var httpConnectionId = Interlocked.Increment(ref _lastHttpConnectionId);

            var httpConnectionContext = new HttpConnectionContext
            {
                ConnectionId = connectionContext.ConnectionId,
                HttpConnectionId = httpConnectionId,
                Protocols = _protocols,
                ServiceContext = _serviceContext,
                ConnectionFeatures = connectionContext.Features,
                BufferPool = connectionContext.BufferPool,
                ConnectionAdapters = _connectionAdapters,
                Transport = connectionContext.Transport,
                Application = transportFeature.Application
            };

            var connectionFeature = connectionContext.Features.Get<IHttpConnectionFeature>();

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

            var processingTask = connection.StartRequestProcessing(_application);

            var inputTcs = new TaskCompletionSource<object>();
            var outputTcs = new TaskCompletionSource<object>();

            // The reason we don't fire events directly from these callbacks is because it seems
            // like the transport callbacks root the state object (even after it fires)
            connectionContext.Transport.Input.OnWriterCompleted(_completeTcs, inputTcs);
            connectionContext.Transport.Output.OnReaderCompleted(_completeTcs, outputTcs);

            inputTcs.Task.ContinueWith((task, state) =>
            {
                ((HttpConnection)state).Abort(task.Exception?.InnerException);
            },
            connection, TaskContinuationOptions.ExecuteSynchronously);

            outputTcs.Task.ContinueWith((task, state) =>
            {
                ((HttpConnection)state).OnConnectionClosed(task.Exception?.InnerException);
            },
            connection, TaskContinuationOptions.ExecuteSynchronously);

            return processingTask;
        }

        private static void CompleteTcs(Exception error, object state)
        {
            var tcs = (TaskCompletionSource<object>)state;

            if (error != null)
            {
                tcs.TrySetException(error);
            }
            else
            {
                tcs.TrySetResult(null);
            }
        }
    }
}
