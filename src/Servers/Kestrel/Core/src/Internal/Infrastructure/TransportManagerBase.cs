// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Net;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal abstract class TransportManagerBase
{
    private readonly List<ActiveTransport> _transports = new List<ActiveTransport>();

    private readonly ServiceContext _serviceContext;

    protected TransportManagerBase(ServiceContext serviceContext)
    {
        _serviceContext = serviceContext;
    }

    protected KestrelTrace Trace => _serviceContext.Log;

    public abstract bool HasFactories { get; }

    protected static bool CanBindFactory(EndPoint endPoint, IConnectionListenerFactorySelector? selector)
    {
        // By default, the last registered factory binds to the endpoint.
        // A factory can implement IConnectionListenerFactorySelector to decide whether it can bind to the endpoint.
        return selector?.CanBind(endPoint) ?? true;
    }

    protected void StartAcceptLoop<T>(IConnectionListener<T> connectionListener, Func<T, Task> connectionDelegate, EndpointConfig? endpointConfig) where T : BaseConnectionContext
    {
        var transportConnectionManager = new TransportConnectionManager(_serviceContext.ConnectionManager);
        var connectionDispatcher = new ConnectionDispatcher<T>(_serviceContext, connectionDelegate, transportConnectionManager);
        var acceptLoopTask = connectionDispatcher.StartAcceptingConnections(connectionListener);

        _transports.Add(new ActiveTransport(connectionListener, acceptLoopTask, transportConnectionManager, endpointConfig));
    }

    public Task StopEndpointsAsync(List<EndpointConfig> endpointsToStop, CancellationToken cancellationToken)
    {
        var transportsToStop = new List<ActiveTransport>();
        foreach (var t in _transports)
        {
            if (t.EndpointConfig is not null && endpointsToStop.Contains(t.EndpointConfig))
            {
                transportsToStop.Add(t);
            }
        }
        return StopTransportsAsync(transportsToStop, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return StopTransportsAsync(new List<ActiveTransport>(_transports), cancellationToken);
    }

    private async Task StopTransportsAsync(List<ActiveTransport> transportsToStop, CancellationToken cancellationToken)
    {
        var tasks = new Task[transportsToStop.Count];

        for (int i = 0; i < transportsToStop.Count; i++)
        {
            tasks[i] = transportsToStop[i].UnbindAsync(cancellationToken);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);

        async Task StopTransportConnection(ActiveTransport transport)
        {
            if (!await transport.TransportConnectionManager.CloseAllConnectionsAsync(cancellationToken).ConfigureAwait(false))
            {
                Trace.NotAllConnectionsClosedGracefully();

                if (!await transport.TransportConnectionManager.AbortAllConnectionsAsync().ConfigureAwait(false))
                {
                    Trace.NotAllConnectionsAborted();
                }
            }
        }

        for (int i = 0; i < transportsToStop.Count; i++)
        {
            tasks[i] = StopTransportConnection(transportsToStop[i]);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);

        for (int i = 0; i < transportsToStop.Count; i++)
        {
            tasks[i] = transportsToStop[i].DisposeAsync().AsTask();
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);

        foreach (var transport in transportsToStop)
        {
            _transports.Remove(transport);
        }
    }

    private sealed class ActiveTransport : IAsyncDisposable
    {
        public ActiveTransport(IConnectionListenerBase transport, Task acceptLoopTask, TransportConnectionManager transportConnectionManager, EndpointConfig? endpointConfig = null)
        {
            ConnectionListener = transport;
            AcceptLoopTask = acceptLoopTask;
            TransportConnectionManager = transportConnectionManager;
            EndpointConfig = endpointConfig;
        }

        public IConnectionListenerBase ConnectionListener { get; }
        public Task AcceptLoopTask { get; }
        public TransportConnectionManager TransportConnectionManager { get; }

        public EndpointConfig? EndpointConfig { get; }

        public async Task UnbindAsync(CancellationToken cancellationToken)
        {
            await ConnectionListener.UnbindAsync(cancellationToken).ConfigureAwait(false);
            await AcceptLoopTask.ConfigureAwait(false);
        }

        public ValueTask DisposeAsync()
        {
            return ConnectionListener.DisposeAsync();
        }
    }
}
