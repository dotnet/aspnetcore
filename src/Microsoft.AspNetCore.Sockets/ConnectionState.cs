using System;
using Channels;

namespace Microsoft.AspNetCore.Sockets
{
    public class ConnectionState
    {
        public DateTimeOffset LastSeen { get; set; }
        public bool Active { get; set; } = true;
        public Connection Connection { get; set; }
    }

    public class Connection
    {
        public string ConnectionId { get; set; }
        public IChannel Channel { get; set; }
    }
}
