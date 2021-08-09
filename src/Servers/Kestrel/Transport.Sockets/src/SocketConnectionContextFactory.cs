// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        /// <summary>
        /// Creates the <see cref="SocketConnectionContextFactory"/>.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="loggerfactory">The logger factory.</param>
        public SocketConnectionContextFactory(SocketConnectionOptions options, ILoggerFactory loggerfactory)
        {
            _options = options;
        }

        /// <summary>
        /// Create a <see cref="ConnectionContext"/> for a socket.
        /// </summary>
        /// <param name="socket">The socket for the connection.</param>
        /// <returns></returns>
        public ConnectionContext Create(Socket socket)
            => new SocketConnection(socket,
                _options.MemoryPool,
                _options.Scheduler,
                _options.Trace,
                _options.SenderPool,
                _options.InputOptions,
                _options.OutputOptions,
                _options.WaitForDataBeforeAllocatingBuffer);

        public void Dispose() { }
    }
}
