// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal
{
    internal interface ISocketsTrace : ILogger
    {
        void ConnectionReadFin(SocketConnection connection);

        void ConnectionWriteFin(SocketConnection connection, string reason);

        void ConnectionError(SocketConnection connection, Exception ex);

        void ConnectionReset(string connectionId);
        void ConnectionReset(SocketConnection connection);

        void ConnectionPause(SocketConnection connection);

        void ConnectionResume(SocketConnection connection);
    }
}
