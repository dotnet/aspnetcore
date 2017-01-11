// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Sockets.Internal
{
    public class ConnectionState : IDisposable
    {
        public Connection Connection { get; set; }
        public IChannelConnection<Message> Application { get; }

        // These are used for long polling mostly
        public Action Close { get; set; }
        public DateTime LastSeenUtc { get; set; }
        public bool Active { get; set; } = true;

        public ConnectionState(Connection connection, IChannelConnection<Message> application)
        {
            Connection = connection;
            Application = application;
            LastSeenUtc = DateTime.UtcNow;
        }

        public void Dispose()
        {
            Connection.Dispose();
            Application.Dispose();
        }
    }
}
