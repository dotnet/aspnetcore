// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal
{
    /// <summary>
    /// Listens for new Quic Connections.
    /// </summary>
    internal class QuicConnectionListener : IConnectionListener, IAsyncDisposable, IDisposable
    {
        private IQuicTrace _log;
        private bool _disposed;
        private QuicTransportContext _context;
        private QuicListener _listener;

        public QuicConnectionListener(QuicTransportOptions options, IHostApplicationLifetime lifetime, IQuicTrace log, EndPoint endpoint)
        {
            _log = log;
            _context = new QuicTransportContext(lifetime, _log, options);
            EndPoint = endpoint;
            var sslConfig = new SslServerAuthenticationOptions();
            sslConfig.ServerCertificate = options.Certificate;
            sslConfig.ApplicationProtocols = new List<SslApplicationProtocol>() { new SslApplicationProtocol(options.Alpn) };
            _listener = new QuicListener(QuicImplementationProviders.MsQuic, endpoint as IPEndPoint, sslConfig);
            _listener.Start();
        }

        public EndPoint EndPoint { get; set; }

        public async ValueTask<ConnectionContext> AcceptAsync(CancellationToken cancellationToken = default)
        {
            var quicConnection = await _listener.AcceptConnectionAsync(cancellationToken);
            return new QuicConnectionContext(quicConnection, _context);
        }

        public async ValueTask UnbindAsync(CancellationToken cancellationToken = default)
        {
            await DisposeAsync();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return new ValueTask();
            }

            Dispose(true);

            return new ValueTask();
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _listener.Dispose();

            _disposed = true;
        }
    }
}
