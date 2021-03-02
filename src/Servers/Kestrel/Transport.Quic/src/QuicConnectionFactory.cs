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
using Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic
{
    internal class QuicConnectionFactory : IMultiplexedConnectionFactory
    {
        private readonly QuicTransportContext _transportContext;

        public QuicConnectionFactory(IOptions<QuicTransportOptions> options, ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Client");
            var trace = new QuicTrace(logger);

            _transportContext = new QuicTransportContext(trace, options.Value);
        }

        public async ValueTask<MultiplexedConnectionContext> ConnectAsync(EndPoint endPoint, IFeatureCollection? features = null, CancellationToken cancellationToken = default)
        {
            if (endPoint is not IPEndPoint)
            {
                throw new NotSupportedException($"{endPoint} is not supported");
            }
            if (_transportContext.Options.Alpn == null)
            {
                throw new InvalidOperationException("QuicTransportOptions.Alpn must be configured with a value.");
            }

            var sslOptions = new SslClientAuthenticationOptions();
            sslOptions.ApplicationProtocols = new List<SslApplicationProtocol>() { new SslApplicationProtocol(_transportContext.Options.Alpn) };
            var connection = new QuicConnection(QuicImplementationProviders.MsQuic, (IPEndPoint)endPoint, sslOptions);

            await connection.ConnectAsync(cancellationToken);
            return new QuicConnectionContext(connection, _transportContext);
        }
    }
}
