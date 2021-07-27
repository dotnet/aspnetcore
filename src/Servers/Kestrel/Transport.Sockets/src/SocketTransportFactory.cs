// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets
{
    /// <summary>
    /// A factory for socket based connections.
    /// </summary>
    public sealed class SocketTransportFactory : IConnectionListenerFactory
    {
        private readonly SocketTransportOptions _transportOptions;
        private readonly ISocketConnectionContextFactory _contextFactory;
        private readonly SocketsTrace _trace;

        public SocketTransportFactory(
            IOptions<SocketTransportOptions> transportOptions,
            ILoggerFactory loggerFactory,
            ISocketConnectionContextFactory contextFactory)
        {
            if (transportOptions == null)
            {
                throw new ArgumentNullException(nameof(transportOptions));
            }

            if (contextFactory == null)
            {
                throw new ArgumentNullException(nameof(contextFactory));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _transportOptions = transportOptions.Value;
            _contextFactory = contextFactory;
            var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets");
            _trace = new SocketsTrace(logger);
        }

        public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            var transport = new SocketConnectionListener(endpoint, _transportOptions, _contextFactory, _trace);
            transport.Bind();
            return new ValueTask<IConnectionListener>(transport);
        }
    }
}
