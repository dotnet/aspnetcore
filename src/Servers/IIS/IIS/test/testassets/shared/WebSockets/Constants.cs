// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    public static class Constants
    {
        public static class Headers
        {
            public const string Upgrade = "Upgrade";
            public const string UpgradeWebSocket = "websocket";
            public const string Connection = "Connection";
            public const string SecWebSocketKey = "Sec-WebSocket-Key";
            public const string SecWebSocketAccept = "Sec-WebSocket-Accept";
        }
    }
}
