// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Net;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal sealed class TransportManager
{
    private readonly List<ActiveTransport> _transports = new List<ActiveTransport>();

    private readonly List<IConnectionListenerFactory> _transportFactories;
    private readonly List<IMultiplexedConnectionListenerFactory> _multiplexedTransportFactories;
    private readonly IHttpsConfigurationService _httpsConfigurationService;
    private readonly ServiceContext _serviceContext;

    public TransportManager(
        List<IConnectionListenerFactory> transportFactories,
        List<IMultiplexedConnectionListenerFactory> multiplexedTransportFactories,
        IHttpsConfigurationService httpsConfigurationService,
        ServiceContext serviceContext)
    {
        _transportFactories = transportFactories;
        _multiplexedTransportFactories = multiplexedTransportFactories;
        _httpsConfigurationService = httpsConfigurationService;
        _serviceContext = serviceContext;
    }

    private ConnectionManager ConnectionManager => _serviceContext.ConnectionManager;
    private KestrelTrace Trace => _serviceContext.Log;

    public async Task<EndPoint> BindAsync(EndPoint endPoint, ConnectionDelegate connectionDelegate, EndpointConfig? endpointConfig, CancellationToken cancellationToken)
    {
        if (_transportFactories.Count == 0)
        {
            throw new InvalidOperationException($"Cannot bind with {nameof(ConnectionDelegate)} no {nameof(IConnectionListenerFactory)} is registered.");
        }

        foreach (var transportFactory in _transportFactories)
        {
            var selector = transportFactory as IConnectionListenerFactorySelector;
            if (CanBindFactory(endPoint, selector))
            {
                var transport = await transportFactory.BindAsync(endPoint, cancellationToken).ConfigureAwait(false);
                StartAcceptLoop(new GenericConnectionListener(transport), c => connectionDelegate(c), endpointConfig);
                return transport.EndPoint;
            }
        }

        // Special case situation where a named pipe endpoint is specified and there is no matching transport.
        // The named pipe transport is only registered on Windows. The test is done at this point so there is
        // the opportunity for the app to register their own transport to handle named pipe endpoints.
        if (endPoint is NamedPipeEndPoint && !OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Named pipes transport requires a Windows operating system.");
        }

        throw new InvalidOperationException($"No registered {nameof(IConnectionListenerFactory)} supports endpoint {endPoint.GetType().Name}: {endPoint}");
    }

    public async Task<EndPoint> BindAsync(EndPoint endPoint, MultiplexedConnectionDelegate multiplexedConnectionDelegate, ListenOptions listenOptions, CancellationToken cancellationToken)
    {
        if (_multiplexedTransportFactories.Count == 0)
        {
            throw new InvalidOperationException($"Cannot bind with {nameof(MultiplexedConnectionDelegate)} no {nameof(IMultiplexedConnectionListenerFactory)} is registered.");
        }

        var features = new FeatureCollection();

        // Will throw an appropriate error if it's not enabled
        _httpsConfigurationService.PopulateMultiplexedTransportFeatures(features, listenOptions);

        foreach (var multiplexedTransportFactory in _multiplexedTransportFactories)
        {
            var selector = multiplexedTransportFactory as IConnectionListenerFactorySelector;
            if (CanBindFactory(endPoint, selector))
            {
                var transport = await multiplexedTransportFactory.BindAsync(endPoint, features, cancellationToken).ConfigureAwait(false);
                StartAcceptLoop(new GenericMultiplexedConnectionListener(transport), c => multiplexedConnectionDelegate(c), listenOptions.EndpointConfig);
                return transport.EndPoint;
            }
        }

        throw new InvalidOperationException($"No registered {nameof(IMultiplexedConnectionListenerFactory)} supports endpoint {endPoint.GetType().Name}: {endPoint}");
    }

    private static bool CanBindFactory(EndPoint endPoint, IConnectionListenerFactorySelector? selector)
    {
        // By default, the last registered factory binds to the endpoint.
        // A factory can implement IConnectionListenerFactorySelector to decide whether it can bind to the endpoint.
        return selector?.CanBind(endPoint) ?? true;
    }

    private void StartAcceptLoop<T>(IConnectionListener<T> connectionListener, Func<T, Task> connectionDelegate, EndpointConfig? endpointConfig) where T : BaseConnectionContext
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

    private sealed class GenericConnectionListener : IConnectionListener<ConnectionContext>
    {
        private readonly IConnectionListener _connectionListener;

        public GenericConnectionListener(IConnectionListener connectionListener)
        {
            _connectionListener = connectionListener;
        }

        public EndPoint EndPoint => _connectionListener.EndPoint;

        public ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
             => _connectionListener.AcceptAsync(cancellationToken);

        public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
            => _connectionListener.UnbindAsync(cancellationToken);

        public ValueTask DisposeAsync()
            => _connectionListener.DisposeAsync();
    }

    private sealed class GenericMultiplexedConnectionListener : IConnectionListener<MultiplexedConnectionContext>
    {
        private readonly IMultiplexedConnectionListener _multiplexedConnectionListener;

        public GenericMultiplexedConnectionListener(IMultiplexedConnectionListener multiplexedConnectionListener)
        {
            _multiplexedConnectionListener = multiplexedConnectionListener;
        }

        public EndPoint EndPoint => _multiplexedConnectionListener.EndPoint;

        public ValueTask<MultiplexedConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
             => _multiplexedConnectionListener.AcceptAsync(features: null, cancellationToken);

        public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
            => _multiplexedConnectionListener.UnbindAsync(cancellationToken);

        public ValueTask DisposeAsync()
            => _multiplexedConnectionListener.DisposeAsync();
    }
}
