// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets
{
    /// <summary>
    /// Defines an interface that provides the mechanisms to create a socket based <see cref="ConnectionContext"/>.
    /// </summary>
    public interface ISocketConnectionContextFactory
    {
        /// <summary>
        /// Create a <see cref="ConnectionContext"/> for a socket.
        /// </summary>
        /// <param name="socket">The socket for the connection.</param>
        /// <param name="options">The <see cref="SocketConnectionOptions"/>.</param>
        /// <returns></returns>
        ConnectionContext Create(Socket socket, SocketConnectionOptions options);
    }
}
