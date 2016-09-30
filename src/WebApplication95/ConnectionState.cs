using System;

namespace WebApplication95
{
    public class ConnectionState 
    {
        public DateTimeOffset LastSeen { get; set; }
        public bool Alive { get; set; } = true;
        public Connection Connection { get; set; }
    }
}
