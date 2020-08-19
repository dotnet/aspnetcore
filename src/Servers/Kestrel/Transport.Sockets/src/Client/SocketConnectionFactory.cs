// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets
{
    public class SocketConnectionFactory : IConnectionFactory, IAsyncDisposable
    {
        private readonly SocketTransportOptions _options;
        private readonly MemoryPool<byte> _memoryPool;
        private readonly SocketsTrace _trace;

        public SocketConnectionFactory(IOptions<SocketTransportOptions> options, ILoggerFactory loggerFactory)
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
            _memoryPool = options.Value.MemoryPoolFactory();
            var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Client");
            _trace = new SocketsTrace(logger);
        }

        public async ValueTask<ConnectionContext> ConnectAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            var ipEndPoint = endpoint as IPEndPoint;

            if (ipEndPoint is null)
            {
                throw new NotSupportedException("The SocketConnectionFactory only supports IPEndPoints for now.");
            }

            var socket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = _options.NoDelay
            };

            await socket.ConnectAsync(ipEndPoint);

            var socketConnection = new SocketConnection(
                socket,
                _memoryPool,
                PipeScheduler.ThreadPool,
                _trace,
                _options.MaxReadBufferSize,
                _options.MaxWriteBufferSize,
                _options.WaitForDataBeforeAllocatingBuffer);

            socketConnection.Start();
            return socketConnection;
        }

        public ValueTask DisposeAsync()
        {
            _memoryPool.Dispose();
            return default;
        }
    }
}
