// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
