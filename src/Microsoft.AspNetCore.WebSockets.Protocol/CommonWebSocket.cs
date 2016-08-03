// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.WebSockets;

namespace Microsoft.AspNetCore.WebSockets.Protocol
{
    public static class CommonWebSocket
    {
        public static WebSocket CreateClientWebSocket(Stream stream, string subProtocol, TimeSpan keepAliveInterval, int receiveBufferSize)
        {
            return ManagedWebSocket.CreateFromConnectedStream(
                stream,
                isServer: false,
                subprotocol: subProtocol,
                keepAliveIntervalSeconds: (int)keepAliveInterval.TotalSeconds,
                receiveBufferSize: receiveBufferSize);
        }

        public static WebSocket CreateServerWebSocket(Stream stream, string subProtocol, TimeSpan keepAliveInterval, int receiveBufferSize)
        {
            return ManagedWebSocket.CreateFromConnectedStream(
                stream,
                isServer: true,
                subprotocol: subProtocol,
                keepAliveIntervalSeconds: (int)keepAliveInterval.TotalSeconds,
                receiveBufferSize: receiveBufferSize);
        }
    }
}
