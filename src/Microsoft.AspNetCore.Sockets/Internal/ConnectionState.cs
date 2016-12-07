// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Sockets.Internal
{
    public abstract class ConnectionState : IDisposable
    {
        public Connection Connection { get; set; }
        public ConnectionMode Mode => Connection.Mode;

        // These are used for long polling mostly
        public Action Close { get; set; }
        public DateTime LastSeenUtc { get; set; }
        public bool Active { get; set; } = true;

        protected ConnectionState(Connection connection)
        {
            Connection = connection;
            LastSeenUtc = DateTime.UtcNow;
        }

        public abstract void Dispose();

        public abstract void TerminateTransport(Exception innerException);
    }
}
