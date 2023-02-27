// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Security;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal sealed class MultiplexedTransportManager : TransportManagerBase, IMultiplexedTransportManager
{
    private readonly List<IMultiplexedConnectionListenerFactory> _factories;

    public MultiplexedTransportManager(
        ServiceContext serviceContext,
        IEnumerable<IMultiplexedConnectionListenerFactory> factories)
        : base(serviceContext)
    {
        _factories = factories.Reverse().ToList();
    }

    public override bool HasFactories => _factories.Count > 0;

    public async Task<EndPoint> BindAsync(EndPoint endPoint, MultiplexedConnectionDelegate multiplexedConnectionDelegate, ListenOptions listenOptions, CancellationToken cancellationToken)
    {
        if (!HasFactories)
        {
            throw new InvalidOperationException($"Cannot bind with {nameof(MultiplexedConnectionDelegate)} no {nameof(IMultiplexedConnectionListenerFactory)} is registered.");
        }

        var features = new FeatureCollection();

        // HttpsOptions or HttpsCallbackOptions should always be set in production, but it's not set for InMemory tests.
        // The QUIC transport will check if TlsConnectionCallbackOptions is missing.
        if (listenOptions.HttpsOptions != null)
        {
            var sslServerAuthenticationOptions = HttpsConnectionMiddleware.CreateHttp3Options(listenOptions.HttpsOptions);
            features.Set(new TlsConnectionCallbackOptions
            {
                ApplicationProtocols = sslServerAuthenticationOptions.ApplicationProtocols ?? new List<SslApplicationProtocol> { SslApplicationProtocol.Http3 },
                OnConnection = (context, cancellationToken) => ValueTask.FromResult(sslServerAuthenticationOptions),
                OnConnectionState = null,
            });
        }
        else if (listenOptions.HttpsCallbackOptions != null)
        {
            features.Set(new TlsConnectionCallbackOptions
            {
                ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http3 },
                OnConnection = (context, cancellationToken) =>
                {
                    return listenOptions.HttpsCallbackOptions.OnConnection(new TlsHandshakeCallbackContext
                    {
                        ClientHelloInfo = context.ClientHelloInfo,
                        CancellationToken = cancellationToken,
                        State = context.State,
                        Connection = new ConnectionContextAdapter(context.Connection),
                    });
                },
                OnConnectionState = listenOptions.HttpsCallbackOptions.OnConnectionState,
            });
        }

        foreach (var factory in _factories)
        {
            var selector = factory as IConnectionListenerFactorySelector;
            if (CanBindFactory(endPoint, selector))
            {
                var transport = await factory.BindAsync(endPoint, features, cancellationToken).ConfigureAwait(false);
                StartAcceptLoop(new GenericMultiplexedConnectionListener(transport), c => multiplexedConnectionDelegate(c), listenOptions.EndpointConfig);
                return transport.EndPoint;
            }
        }

        throw new InvalidOperationException($"No registered {nameof(IMultiplexedConnectionListenerFactory)} supports endpoint {endPoint.GetType().Name}: {endPoint}");
    }

    /// <summary>
    /// TlsHandshakeCallbackContext.Connection is ConnectionContext but QUIC connection only implements BaseConnectionContext.
    /// </summary>
    private sealed class ConnectionContextAdapter : ConnectionContext
    {
        private readonly BaseConnectionContext _inner;

        public ConnectionContextAdapter(BaseConnectionContext inner) => _inner = inner;

        public override IDuplexPipe Transport
        {
            get => throw new NotSupportedException("Not supported by HTTP/3 connections.");
            set => throw new NotSupportedException("Not supported by HTTP/3 connections.");
        }
        public override string ConnectionId
        {
            get => _inner.ConnectionId;
            set => _inner.ConnectionId = value;
        }
        public override IFeatureCollection Features => _inner.Features;
        public override IDictionary<object, object?> Items
        {
            get => _inner.Items;
            set => _inner.Items = value;
        }
        public override EndPoint? LocalEndPoint
        {
            get => _inner.LocalEndPoint;
            set => _inner.LocalEndPoint = value;
        }
        public override EndPoint? RemoteEndPoint
        {
            get => _inner.RemoteEndPoint;
            set => _inner.RemoteEndPoint = value;
        }
        public override CancellationToken ConnectionClosed
        {
            get => _inner.ConnectionClosed;
            set => _inner.ConnectionClosed = value;
        }
        public override ValueTask DisposeAsync() => _inner.DisposeAsync();
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
