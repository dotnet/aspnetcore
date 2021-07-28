// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets
{
    /// <summary>
    /// A factory for socket based connections contexts.
    /// </summary>
    public sealed class SocketConnectionContextFactory : ISocketConnectionContextFactory
    {
        private readonly SocketTransportOptions _transportOptions;
        private readonly PipeScheduler _transportScheduler;
        private readonly ISocketsTrace _trace;

        public SocketConnectionContextFactory(
            IOptions<SocketTransportOptions> transportOptions,
            ILoggerFactory loggerFactory)
        {
            _transportOptions = transportOptions.Value;

            _transportScheduler = _transportOptions.UnsafePreferInlineScheduling ? PipeScheduler.Inline :
                (_transportOptions.IOQueueCount > 0) ? new IOQueue() : PipeScheduler.ThreadPool;
            // https://github.com/aspnet/KestrelHttpServer/issues/2573

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
            => new SocketConnection(socket,
                options.MemoryPool,
                _transportScheduler,
                _trace,
                options.SenderPool,
                options.InputOptions,
                options.OutputOptions,
                waitForData: _transportOptions.WaitForDataBeforeAllocatingBuffer);
    }
}
