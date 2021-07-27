// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets
{
    /// <summary>
    /// A factory for socket based connections contexts.
    /// </summary>
    internal sealed class SocketConnectionContextFactory : ISocketConnectionContextFactory, IAsyncDisposable
    {
        private readonly MemoryPool<byte> _memoryPool;
        private readonly int _socketSendersCount;
        private readonly SocketSenderPool[] _socketSenders;
        private int _socketSenderIndex;
        private readonly SocketTransportOptions _transportOptions;
        private readonly PipeScheduler _transportScheduler;
        private readonly ISocketsTrace _trace;

        public SocketConnectionContextFactory(
            IOptions<SocketTransportOptions> transportOptions,
            ILoggerFactory loggerFactory)
        {
            _transportOptions = transportOptions.Value;
            _memoryPool = _transportOptions.MemoryPoolFactory();

            _transportScheduler = _transportOptions.UnsafePreferInlineScheduling ? PipeScheduler.Inline : new IOQueue();
            // https://github.com/aspnet/KestrelHttpServer/issues/2573
            var awaiterScheduler = OperatingSystem.IsWindows() ? _transportScheduler : PipeScheduler.Inline;

            var ioQueueCount = _transportOptions.IOQueueCount;
            _socketSendersCount = (ioQueueCount > 0) ? ioQueueCount : 1;
            _socketSenders = new SocketSenderPool[_socketSendersCount];
            for (var i = 0; i < _socketSendersCount; i++)
            {
                _socketSenders[i] = new SocketSenderPool(awaiterScheduler);
            }

            var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets");
            _trace = new SocketsTrace(logger);
        }

        /// <summary>
        /// Create a <see cref="ConnectionContext"/> for a socket.
        /// </summary>
        /// <param name="socket">The socket for the connection.</param>
        /// <param name="options">The <see cref="SocketConnectionOptions"/>.</param>
        /// <returns></returns>
        public ConnectionContext Create(Socket socket, SocketConnectionOptions options)
        {
            var setting = _socketSenders[_socketSenderIndex];

            var connection = new SocketConnection(socket,
                _memoryPool,
                _transportScheduler,
                _trace,
                setting,
                options.InputOptions,
                options.OutputOptions,
                waitForData: _transportOptions.WaitForDataBeforeAllocatingBuffer);

            _socketSenderIndex = (_socketSenderIndex + 1) % _socketSendersCount;

            return connection;
        }

        public ValueTask DisposeAsync()
        {
            // Dispose any pooled senders
            foreach (var sender in _socketSenders)
            {
                sender.Dispose();
            }

            return default;
        }
    }
}
