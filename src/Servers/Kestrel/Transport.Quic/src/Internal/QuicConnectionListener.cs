// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal
{
    /// <summary>
    /// Listens for new Quic Connections.
    /// </summary>
    internal class QuicConnectionListener : IMultiplexedConnectionListener, IAsyncDisposable
    {
        private readonly IQuicTrace _log;
        private bool _disposed;
        private readonly QuicTransportContext _context;
        private readonly QuicListener _listener;

        public QuicConnectionListener(QuicTransportOptions options, IQuicTrace log, EndPoint endpoint)
        {
            _log = log;
            _context = new QuicTransportContext(_log, options);
            EndPoint = endpoint;
            var sslConfig = new SslServerAuthenticationOptions();
            sslConfig.ServerCertificate = options.Certificate;
            sslConfig.ApplicationProtocols = new List<SslApplicationProtocol>() { new SslApplicationProtocol(options.Alpn) };
            _listener = new QuicListener(QuicImplementationProviders.MsQuic, endpoint as IPEndPoint, sslConfig);
            _listener.Start();
        }

        public EndPoint EndPoint { get; set; }

        public async ValueTask<MultiplexedConnectionContext> AcceptAsync(IFeatureCollection features = null, CancellationToken cancellationToken = default)
        {
            var quicConnection = await _listener.AcceptConnectionAsync(cancellationToken);
            try
            {
                _ = quicConnection.LocalEndPoint;
            }
            catch (Exception)
            {
                return null;
            }

            return new QuicConnectionContext(quicConnection, _context);
        }

        public async ValueTask UnbindAsync(CancellationToken cancellationToken = default)
        {
            await DisposeAsync();
        }

        public ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return new ValueTask();
            }

            _disposed = true;

            _listener.Close();
            _listener.Dispose();

            return new ValueTask();
        }
    }
}
