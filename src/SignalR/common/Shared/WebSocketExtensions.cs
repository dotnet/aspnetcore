// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal static class WebSocketExtensions
    {
        public static ValueTask SendAsync(this WebSocket webSocket, ReadOnlySequence<byte> buffer, WebSocketMessageType webSocketMessageType, CancellationToken cancellationToken = default)
        {
#if NETCOREAPP
            if (buffer.IsSingleSegment)
            {
                return webSocket.SendAsync(buffer.First, webSocketMessageType, endOfMessage: true, cancellationToken);
            }
            else
            {
                return SendMultiSegmentAsync(webSocket, buffer, webSocketMessageType, cancellationToken);
            }
#else
            if (buffer.IsSingleSegment)
            {
                var isArray = MemoryMarshal.TryGetArray(buffer.First, out var segment);
                Debug.Assert(isArray);
                return new ValueTask(webSocket.SendAsync(segment, webSocketMessageType, endOfMessage: true, cancellationToken));
            }
            else
            {
                return SendMultiSegmentAsync(webSocket, buffer, webSocketMessageType, cancellationToken);
            }
#endif
        }

        private static async ValueTask SendMultiSegmentAsync(WebSocket webSocket, ReadOnlySequence<byte> buffer, WebSocketMessageType webSocketMessageType, CancellationToken cancellationToken = default)
        {
            var position = buffer.Start;
            // Get a segment before the loop so we can be one segment behind while writing
            // This allows us to do a non-zero byte write for the endOfMessage = true send
            buffer.TryGet(ref position, out var prevSegment);
            while (buffer.TryGet(ref position, out var segment))
            {
#if NETCOREAPP
                await webSocket.SendAsync(prevSegment, webSocketMessageType, endOfMessage: false, cancellationToken);
#else
                var isArray = MemoryMarshal.TryGetArray(prevSegment, out var arraySegment);
                Debug.Assert(isArray);
                await webSocket.SendAsync(arraySegment, webSocketMessageType, endOfMessage: false, cancellationToken);
#endif
                prevSegment = segment;
            }

            // End of message frame
#if NETCOREAPP
            await webSocket.SendAsync(prevSegment, webSocketMessageType, endOfMessage: true, cancellationToken);
#else
            var isArrayEnd = MemoryMarshal.TryGetArray(prevSegment, out var arraySegmentEnd);
            Debug.Assert(isArrayEnd);
            await webSocket.SendAsync(arraySegmentEnd, webSocketMessageType, endOfMessage: true, cancellationToken);
#endif
        }
    }
}
