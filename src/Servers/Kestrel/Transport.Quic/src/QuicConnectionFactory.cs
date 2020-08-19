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
using Microsoft.AspNetCore.Server.Kestrel.Transport.Experimental.Quic.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Experimental.Quic
{
    public class QuicConnectionFactory : IMultiplexedConnectionFactory
    {
        private QuicTransportContext _transportContext;

        public QuicConnectionFactory(IOptions<QuicTransportOptions> options, ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.Transport.Experimental.Quic.Client");
            var trace = new QuicTrace(logger);

            _transportContext = new QuicTransportContext(trace, options.Value);
        }

        public async ValueTask<MultiplexedConnectionContext> ConnectAsync(EndPoint endPoint, IFeatureCollection features = null, CancellationToken cancellationToken = default)
        {
            if (!(endPoint is IPEndPoint ipEndPoint))
            {
                throw new NotSupportedException($"{endPoint} is not supported");
            }

            var sslOptions = new SslClientAuthenticationOptions();
            sslOptions.ApplicationProtocols = new List<SslApplicationProtocol>() { new SslApplicationProtocol(_transportContext.Options.Alpn) };
            var connection = new QuicConnection(QuicImplementationProviders.MsQuic, endPoint as IPEndPoint, sslOptions);

            await connection.ConnectAsync(cancellationToken);
            return new QuicConnectionContext(connection, _transportContext);
        }
    }
}
