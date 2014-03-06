using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Net.WebSockets
{
    public static class Constants
    {
        public static class Headers
        {
            public const string WebSocketVersion = "Sec-WebSocket-Version";
            public const string SupportedVersion = "13";
        }

        public static class OpCodes
        {
            public const int ContinuationFrame = 0x0;
            public const int TextFrame = 0x1;
            public const int BinaryFrame = 0x2;
            public const int CloseFrame = 0x8;
            public const int PingFrame = 0x9;
            public const int PongFrame = 0xA;            
        }
    }
}
