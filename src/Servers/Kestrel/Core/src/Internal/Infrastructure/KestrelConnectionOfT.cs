using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal class KestrelConnection<T> : KestrelConnection, IThreadPoolWorkItem where T : BaseConnectionContext
    {
        private readonly Func<T, Task> _connectionDelegate;
        private readonly T _transportConnection;

        public KestrelConnection(long id,
                                 ServiceContext serviceContext,
                                 Func<T, Task> connectionDelegate,
                                 T connectionContext,
                                 IKestrelTrace logger)
            : base(id, serviceContext, logger)
        {
            _connectionDelegate = connectionDelegate;
            _transportConnection = connectionContext;
            connectionContext.Features.Set<IConnectionHeartbeatFeature>(this);
            connectionContext.Features.Set<IConnectionCompleteFeature>(this);
            connectionContext.Features.Set<IConnectionLifetimeNotificationFeature>(this);
        }

        public override BaseConnectionContext TransportConnection => _transportConnection;

        void IThreadPoolWorkItem.Execute()
        {
            _ = ExecuteAsync();
        }

        internal async Task ExecuteAsync()
        {
            var connectionContext = _transportConnection;

            try
            {
                Logger.ConnectionStart(connectionContext.ConnectionId);
                KestrelEventSource.Log.ConnectionStart(connectionContext);

                using (BeginConnectionScope(connectionContext))
                {
                    try
                    {
                        await _connectionDelegate(connectionContext);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(0, ex, "Unhandled exception while processing {ConnectionId}.", connectionContext.ConnectionId);
                    }
                }
            }
            finally
            {
                await FireOnCompletedAsync();

                Logger.ConnectionStop(connectionContext.ConnectionId);
                KestrelEventSource.Log.ConnectionStop(connectionContext);

                // Dispose the transport connection, this needs to happen before removing it from the
                // connection manager so that we only signal completion of this connection after the transport
                // is properly torn down.
                await connectionContext.DisposeAsync();

                _serviceContext.ConnectionManager.RemoveConnection(_id);
            }
        }
    }
}
