// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Adapter.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Tls
{
    public class TlsConnectionAdapter : IConnectionAdapter
    {
        private static readonly ClosedAdaptedConnection _closedAdaptedConnection = new ClosedAdaptedConnection();
        private static readonly List<string> _serverProtocols = new List<string>();

        private readonly TlsConnectionAdapterOptions _options;
        private readonly ILogger _logger;

        private string _applicationProtocol;

        public TlsConnectionAdapter(TlsConnectionAdapterOptions options)
            : this(options, loggerFactory: null)
        {
        }

        public TlsConnectionAdapter(TlsConnectionAdapterOptions options, ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.CertificatePath == null)
            {
                throw new ArgumentException("Certificate path must be non-null.", nameof(options));
            }

            if (options.PrivateKeyPath == null)
            {
                throw new ArgumentException("Private key path must be non-null.", nameof(options));
            }

            _options = options;
            _logger = loggerFactory?.CreateLogger(nameof(TlsConnectionAdapter));

            // Order is important. If HTTP/2 is enabled, we prefer it over HTTP/1.1. So add it first.
            if ((options.Protocols & HttpProtocols.Http2) == HttpProtocols.Http2)
            {
                _serverProtocols.Add("h2");
            }

            if ((options.Protocols & HttpProtocols.Http1) == HttpProtocols.Http1)
            {
                _serverProtocols.Add("http/1.1");
            }
        }

        public bool IsHttps => true;

        public Task<IAdaptedConnection> OnConnectionAsync(ConnectionAdapterContext context)
        {
            // Don't trust TlsStream not to block.
            return Task.Run(() => InnerOnConnectionAsync(context));
        }

        private async Task<IAdaptedConnection> InnerOnConnectionAsync(ConnectionAdapterContext context)
        {
            var tlsStream = new TlsStream(context.ConnectionStream, _options.CertificatePath, _options.PrivateKeyPath, _serverProtocols);

            try
            {
                await tlsStream.DoHandshakeAsync();
                _applicationProtocol = tlsStream.GetNegotiatedApplicationProtocol();
            }
            catch (IOException ex)
            {
                _logger?.LogInformation(1, ex, "Authentication failed.");
                tlsStream.Dispose();
                return _closedAdaptedConnection;
            }

            // Always set the feature even though the cert might be null
            context.Features.Set<ITlsConnectionFeature>(new TlsConnectionFeature());
            context.Features.Set<ITlsApplicationProtocolFeature>(new TlsApplicationProtocolFeature(_applicationProtocol));

            return new TlsAdaptedConnection(tlsStream);
        }

        private class TlsAdaptedConnection : IAdaptedConnection
        {
            private readonly TlsStream _tlsStream;

            public TlsAdaptedConnection(TlsStream tlsStream)
            {
                _tlsStream = tlsStream;
            }

            public Stream ConnectionStream => _tlsStream;

            public void Dispose()
            {
                _tlsStream.Dispose();
            }
        }

        private class ClosedAdaptedConnection : IAdaptedConnection
        {
            public Stream ConnectionStream { get; } = new ClosedStream();

            public void Dispose()
            {
            }
        }
    }
}
