using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Extensions.WebSockets
{
    public enum WebSocketConnectionState
    {
        Created,
        Connected,
        CloseSent,
        CloseReceived,
        Closed
    }
}
