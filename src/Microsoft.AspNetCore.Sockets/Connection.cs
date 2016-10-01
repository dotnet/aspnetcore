using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Channels;

namespace Microsoft.AspNetCore.Sockets
{
    public class Connection
    {
        public string ConnectionId { get; set; }
        public IChannel Channel { get; set; }
    }
}
