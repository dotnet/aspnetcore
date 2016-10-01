using System;

namespace Microsoft.AspNetCore.Sockets
{
    public class ConnectionState
    {
        public DateTimeOffset LastSeen { get; set; }
        public bool Active { get; set; } = true;
        public Connection Connection { get; set; }
    }
}
