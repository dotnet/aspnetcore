// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal static class WebSocketExtensions
    {
        public static Task SendAsync(this WebSocket webSocket, ReadOnlyBuffer<byte> buffer, WebSocketMessageType webSocketMessageType, CancellationToken cancellationToken = default)
        {
            // TODO: Consider chunking writes here if we get a multi segment buffer
#if NETCOREAPP2_1
            if (buffer.IsSingleSegment)
            {
                return webSocket.SendAsync(buffer.First, webSocketMessageType, endOfMessage: true, cancellationToken);
            }
            else
            {
                return webSocket.SendAsync(buffer.ToArray(), webSocketMessageType, endOfMessage: true, cancellationToken);
            }
#else
            if (buffer.IsSingleSegment)
            {
                var isArray = MemoryMarshal.TryGetArray(buffer.First, out var segment);
                Debug.Assert(isArray);
                return webSocket.SendAsync(segment, webSocketMessageType, endOfMessage: true, cancellationToken);
            }
            else
            {
                return webSocket.SendAsync(new ArraySegment<byte>(buffer.ToArray()), webSocketMessageType, true, cancellationToken);
            }
#endif
        }
    }
}
