// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets
{
    public sealed class SocketTransportFactory : IConnectionListenerFactory, IConnectionFactory
    {
        private readonly SocketTransportOptions _options;
        private readonly SocketsTrace _trace;

        public SocketTransportFactory(): this(Options.Create(new SocketTransportOptions()), NullLoggerFactory.Instance)
        {

        }

        public SocketTransportFactory(
            IOptions<SocketTransportOptions> options,
            ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _options = options.Value;
            var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets");
            _trace = new SocketsTrace(logger);
        }

        public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint)
        {
            var transport = new SocketConnectionListener(endpoint, _options.IOQueueCount, _trace, _options.MemoryPoolFactory());
            transport.Bind();
            return new ValueTask<IConnectionListener>(transport);
        }

        public async ValueTask<ConnectionContext> ConnectAsync(EndPoint endpoint)
        {
            // REVIEW: How do we pick the type of socket? Is the endpoint enough?
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(endpoint);
            return new SocketConnection(socket, _options.MemoryPoolFactory(), PipeScheduler.ThreadPool, _trace);
        }
    }
}
