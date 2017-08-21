using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Protocols;
using Microsoft.AspNetCore.Protocols.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    public class HttpConnectionMiddleware<TContext>
    {
        private static long _lastFrameConnectionId = long.MinValue;

        private readonly IList<IConnectionAdapter> _connectionAdapters;
        private readonly ServiceContext _serviceContext;
        private readonly IHttpApplication<TContext> _application;

        public HttpConnectionMiddleware(IList<IConnectionAdapter> adapters, ServiceContext serviceContext, IHttpApplication<TContext> application)
        {
            _serviceContext = serviceContext;
            _application = application;

            // Keeping these around for now so progress can be made without updating tests
            _connectionAdapters = adapters;
        }

        public Task OnConnectionAsync(ConnectionContext connectionContext)
        {
            // We need the transport feature so that we can cancel the output reader that the transport is using
            // This is a bit of a hack but it preserves the existing semantics
            var transportFeature = connectionContext.Features.Get<IConnectionTransportFeature>();

            var frameConnectionId = Interlocked.Increment(ref _lastFrameConnectionId);

            var frameConnectionContext = new FrameConnectionContext
            {
                ConnectionId = connectionContext.ConnectionId,
                FrameConnectionId = frameConnectionId,
                ServiceContext = _serviceContext,
                PipeFactory = connectionContext.PipeFactory,
                ConnectionAdapters = _connectionAdapters,
                Transport = connectionContext.Transport,
                Application = transportFeature.Application
            };

            var connectionFeature = connectionContext.Features.Get<IHttpConnectionFeature>();

            if (connectionFeature != null)
            {
                if (connectionFeature.LocalIpAddress != null)
                {
                    frameConnectionContext.LocalEndPoint = new IPEndPoint(connectionFeature.LocalIpAddress, connectionFeature.LocalPort);
                }

                if (connectionFeature.RemoteIpAddress != null)
                {
                    frameConnectionContext.RemoteEndPoint = new IPEndPoint(connectionFeature.RemoteIpAddress, connectionFeature.RemotePort);
                }
            }

            var connection = new FrameConnection(frameConnectionContext);

            // The order here is important, start request processing so that
            // the frame is created before this yields. Events need to be wired up
            // afterwards
            var processingTask = connection.StartRequestProcessing(_application);

            // Wire up the events an forward calls to the frame connection
            // It's important that these execute synchronously because graceful
            // connection close is order sensative (for now)
            connectionContext.ConnectionAborted.ContinueWith((task, state) =>
            {
                // Unwrap the aggregate exception
                ((FrameConnection)state).Abort(task.Exception?.InnerException);
            },
            connection, TaskContinuationOptions.ExecuteSynchronously);

            connectionContext.ConnectionClosed.ContinueWith((task, state) =>
            {
                // Unwrap the aggregate exception
                ((FrameConnection)state).OnConnectionClosed(task.Exception?.InnerException);
            },
            connection, TaskContinuationOptions.ExecuteSynchronously);

            return processingTask;
        }
    }
}
