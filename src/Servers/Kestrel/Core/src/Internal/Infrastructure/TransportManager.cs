// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal class TransportManager
    {
        private readonly List<ActiveTransport> _transports = new List<ActiveTransport>();

        private readonly IConnectionListenerFactory? _transportFactory;
        private readonly IMultiplexedConnectionListenerFactory? _multiplexedTransportFactory;
        private readonly ServiceContext _serviceContext;

        public TransportManager(
            IConnectionListenerFactory? transportFactory,
            IMultiplexedConnectionListenerFactory? multiplexedTransportFactory,
            ServiceContext serviceContext)
        {
            _transportFactory = transportFactory;
            _multiplexedTransportFactory = multiplexedTransportFactory;
            _serviceContext = serviceContext;
        }

        private ConnectionManager ConnectionManager => _serviceContext.ConnectionManager;
        private IKestrelTrace Trace => _serviceContext.Log;

        public async Task<EndPoint> BindAsync(EndPoint endPoint, ConnectionDelegate connectionDelegate, EndpointConfig? endpointConfig, CancellationToken cancellationToken)
        {
            if (_transportFactory is null)
            {
                throw new InvalidOperationException($"Cannot bind with {nameof(ConnectionDelegate)} no {nameof(IConnectionListenerFactory)} is registered.");
            }

            var transport = await _transportFactory.BindAsync(endPoint, cancellationToken).ConfigureAwait(false);
            StartAcceptLoop(new GenericConnectionListener(transport), c => connectionDelegate(c), endpointConfig);
            return transport.EndPoint;
        }

        public async Task<EndPoint> BindAsync(EndPoint endPoint, MultiplexedConnectionDelegate multiplexedConnectionDelegate, ListenOptions listenOptions, CancellationToken cancellationToken)
        {
            if (_multiplexedTransportFactory is null)
            {
                throw new InvalidOperationException($"Cannot bind with {nameof(MultiplexedConnectionDelegate)} no {nameof(IMultiplexedConnectionListenerFactory)} is registered.");
            }

            var features = new FeatureCollection();

            if (listenOptions.HttpsOptions != null)
            {
                // TODO Set other relevant values on options
                var sslServerAuthenticationOptions = new SslServerAuthenticationOptions
                {
                    ServerCertificate = listenOptions.HttpsOptions.ServerCertificate
                };

                features.Set(sslServerAuthenticationOptions);
            }

            var transport = await _multiplexedTransportFactory.BindAsync(endPoint, features, cancellationToken).ConfigureAwait(false);
            StartAcceptLoop(new GenericMultiplexedConnectionListener(transport), c => multiplexedConnectionDelegate(c), listenOptions.EndpointConfig);
            return transport.EndPoint;
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
            var transportsToStop = _transports.Where(t => t.EndpointConfig != null && endpointsToStop.Contains(t.EndpointConfig)).ToList();
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

        private class ActiveTransport : IAsyncDisposable
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

        private class GenericConnectionListener : IConnectionListener<ConnectionContext>
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

        private class GenericMultiplexedConnectionListener : IConnectionListener<MultiplexedConnectionContext>
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
}
