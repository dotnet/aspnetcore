// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets
{
    /// <summary>
    /// A factory for socket based connections contexts.
    /// </summary>
    public sealed class SocketConnectionContextFactory : IDisposable
    {
        private readonly SocketConnectionOptions _options;
        private readonly ISocketsTrace _trace;
        private readonly PipeScheduler _scheduler;
        private readonly SocketSenderPool _senderPool;

        /// <summary>
        /// Creates the <see cref="SocketConnectionContextFactory"/>.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public SocketConnectionContextFactory(SocketConnectionOptions options, ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _options = options;
            var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets");
            _trace = new SocketsTrace(logger);
            _scheduler = options.InputOptions.WriterScheduler;

            // https://github.com/aspnet/KestrelHttpServer/issues/2573
            _senderPool = new SocketSenderPool(OperatingSystem.IsWindows() ? _scheduler : PipeScheduler.Inline);
        }

        /// <summary>
        /// Create a <see cref="ConnectionContext"/> for a socket.
        /// </summary>
        /// <param name="socket">The socket for the connection.</param>
        /// <returns></returns>
        public ConnectionContext Create(Socket socket)
            => new SocketConnection(socket, _options, _scheduler, _senderPool, _trace);

        /// <inheritdoc />
        public void Dispose()
            => _senderPool.Dispose();
    }
}
