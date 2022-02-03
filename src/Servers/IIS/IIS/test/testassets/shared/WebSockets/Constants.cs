// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;

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
