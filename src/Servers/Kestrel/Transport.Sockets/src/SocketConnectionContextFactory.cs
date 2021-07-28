// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets
{
    /// <summary>
    /// A factory for socket based connections contexts.
    /// </summary>
    public sealed class SocketConnectionContextFactory : ISocketConnectionContextFactory
    {
        /// <summary>
        /// Create a <see cref="ConnectionContext"/> for a socket.
        /// </summary>
        /// <param name="socket">The socket for the connection.</param>
        /// <param name="options">The <see cref="SocketConnectionOptions"/>.</param>
        /// <returns></returns>
        public ConnectionContext Create(Socket socket, SocketConnectionOptions options)
            => new SocketConnection(socket,
                options.MemoryPool,
                options.Scheduler,
                options.Trace,
                options.SenderPool,
                options.InputOptions,
                options.OutputOptions,
                waitForData: options.WaitForDataBeforeAllocatingBuffer);
    }
}
