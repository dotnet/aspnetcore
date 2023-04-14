// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal sealed class KestrelConnection<T> : KestrelConnection, IThreadPoolWorkItem where T : BaseConnectionContext
{
    private readonly Func<T, Task> _connectionDelegate;
    private readonly T _transportConnection;

    public KestrelConnection(long id,
                             ServiceContext serviceContext,
                             TransportConnectionManager transportConnectionManager,
                             Func<T, Task> connectionDelegate,
                             T connectionContext,
                             KestrelTrace logger)
        : base(id, serviceContext, transportConnectionManager, logger)
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
        var metricsConnectionDurationEnabled = _serviceContext.Metrics.IsConnectionDurationEnabled();
        var startTimestamp = 0L;
        ConnectionMetricsTagsFeature? metricsTagsFeature = null;
        Exception? unhandledException = null;

        if (metricsConnectionDurationEnabled)
        {
            startTimestamp = Stopwatch.GetTimestamp();

            metricsTagsFeature = new ConnectionMetricsTagsFeature();
            connectionContext.Features.Set<IConnectionMetricsTagsFeature>(metricsTagsFeature);
        }

        try
        {
            KestrelEventSource.Log.ConnectionQueuedStop(connectionContext);
            _serviceContext.Metrics.ConnectionQueuedStop(connectionContext);

            Logger.ConnectionStart(connectionContext.ConnectionId);
            KestrelEventSource.Log.ConnectionStart(connectionContext);
            _serviceContext.Metrics.ConnectionStart(connectionContext);

            using (BeginConnectionScope(connectionContext))
            {
                try
                {
                    await _connectionDelegate(connectionContext);
                }
                catch (Exception ex)
                {
                    unhandledException = ex;
                    Logger.LogError(0, ex, "Unhandled exception while processing {ConnectionId}.", connectionContext.ConnectionId);
                }
            }
        }
        finally
        {
            await FireOnCompletedAsync();

            var currentTimestamp = 0L;
            if (metricsConnectionDurationEnabled)
            {
                currentTimestamp = Stopwatch.GetTimestamp();
            }

            Logger.ConnectionStop(connectionContext.ConnectionId);
            KestrelEventSource.Log.ConnectionStop(connectionContext);
            _serviceContext.Metrics.ConnectionStop(connectionContext, unhandledException, metricsTagsFeature?.TagsList, startTimestamp, currentTimestamp);

            // Dispose the transport connection, this needs to happen before removing it from the
            // connection manager so that we only signal completion of this connection after the transport
            // is properly torn down.
            await connectionContext.DisposeAsync();

            _transportConnectionManager.RemoveConnection(_id);
        }
    }

    private sealed class ConnectionMetricsTagsFeature : IConnectionMetricsTagsFeature
    {
        public ICollection<KeyValuePair<string, object?>> Tags => TagsList;
        public List<KeyValuePair<string, object?>> TagsList { get; } = new List<KeyValuePair<string, object?>>();
    }
}
