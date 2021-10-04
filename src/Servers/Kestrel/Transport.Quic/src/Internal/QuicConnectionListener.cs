// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal
{
    /// <summary>
    /// Listens for new Quic Connections.
    /// </summary>
    internal class QuicConnectionListener : IMultiplexedConnectionListener, IAsyncDisposable
    {
        private readonly QuicTrace _log;
        private bool _disposed;
        private readonly QuicTransportContext _context;
        private readonly QuicListener _listener;

        public QuicConnectionListener(QuicTransportOptions options, QuicTrace log, EndPoint endpoint, SslServerAuthenticationOptions sslServerAuthenticationOptions)
        {
            if (!QuicImplementationProviders.Default.IsSupported)
            {
                throw new NotSupportedException("QUIC is not supported or enabled on this platform. See https://aka.ms/aspnet/kestrel/http3reqs for details.");
            }

            _log = log;
            _context = new QuicTransportContext(_log, options);
            var quicListenerOptions = new QuicListenerOptions();

            var listenEndPoint = endpoint as IPEndPoint;

            if (listenEndPoint == null)
            {
                throw new InvalidOperationException($"QUIC doesn't support listening on the configured endpoint type. Expected {nameof(IPEndPoint)} but got {endpoint.GetType().Name}.");
            }

            // Workaround for issue in System.Net.Quic
            // https://github.com/dotnet/runtime/issues/57241
            if (listenEndPoint.Address.Equals(IPAddress.Any) && listenEndPoint.Address != IPAddress.Any)
            {
                listenEndPoint = new IPEndPoint(IPAddress.Any, listenEndPoint.Port);
            }
            if (listenEndPoint.Address.Equals(IPAddress.IPv6Any) && listenEndPoint.Address != IPAddress.IPv6Any)
            {
                listenEndPoint = new IPEndPoint(IPAddress.IPv6Any, listenEndPoint.Port);
            }

            quicListenerOptions.ServerAuthenticationOptions = sslServerAuthenticationOptions;
            quicListenerOptions.ListenEndPoint = listenEndPoint;
            quicListenerOptions.IdleTimeout = options.IdleTimeout;
            quicListenerOptions.MaxBidirectionalStreams = options.MaxBidirectionalStreamCount;
            quicListenerOptions.MaxUnidirectionalStreams = options.MaxUnidirectionalStreamCount;
            quicListenerOptions.ListenBacklog = options.Backlog;

            _listener = new QuicListener(quicListenerOptions);

            // Listener endpoint will resolve an ephemeral port, e.g. 127.0.0.1:0, into the actual port.
            EndPoint = _listener.ListenEndPoint;
        }

        public EndPoint EndPoint { get; set; }

        public async ValueTask<MultiplexedConnectionContext?> AcceptAsync(IFeatureCollection? features = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var quicConnection = await _listener.AcceptConnectionAsync(cancellationToken);
                var connectionContext = new QuicConnectionContext(quicConnection, _context);

                _log.AcceptedConnection(connectionContext);

                return connectionContext;
            }
            catch (QuicOperationAbortedException ex)
            {
                _log.LogDebug($"Listener has aborted with exception: {ex.Message}");
            }
            return null;
        }

        public async ValueTask UnbindAsync(CancellationToken cancellationToken = default)
        {
            await DisposeAsync();
        }

        public ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return ValueTask.CompletedTask;
            }

            _listener.Dispose();
            _disposed = true;

            return ValueTask.CompletedTask;
        }
    }
}
