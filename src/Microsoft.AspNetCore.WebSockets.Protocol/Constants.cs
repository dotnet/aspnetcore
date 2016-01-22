// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.WebSockets.Protocol
{
    public static class Constants
    {
        public static class Headers
        {
            public const string Upgrade = "Upgrade";
            public const string UpgradeWebSocket = "websocket";
            public const string Connection = "Connection";
            public const string ConnectionUpgrade = "Upgrade";
            public const string SecWebSocketKey = "Sec-WebSocket-Key";
            public const string SecWebSocketVersion = "Sec-WebSocket-Version";
            public const string SecWebSocketProtocol = "Sec-WebSocket-Protocol";
            public const string SecWebSocketAccept = "Sec-WebSocket-Accept";
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

            internal static readonly int[] ValidOpCodes = new int[]
            {
                ContinuationFrame,
                TextFrame,
                BinaryFrame,
                CloseFrame,
                PingFrame,
                PongFrame,
            };
        }
    }
}
