// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Channels;

namespace Microsoft.Extensions.WebSockets.Internal
{
    /// <summary>
    /// Represents a single Frame received or sent on a <see cref="IWebSocketConnection"/>.
    /// </summary>
    public struct WebSocketFrame
    {
        /// <summary>
        /// Indicates if the "FIN" flag is set on this frame, which indicates it is the final frame of a message.
        /// </summary>
        public bool EndOfMessage { get; }

        /// <summary>
        /// Gets the <see cref="WebSocketOpcode"/> value describing the opcode of the WebSocket frame.
        /// </summary>
        public WebSocketOpcode Opcode { get; }

        /// <summary>
        /// Gets the payload of the WebSocket frame.
        /// </summary>
        public ReadableBuffer Payload { get; }

        public WebSocketFrame(bool endOfMessage, WebSocketOpcode opcode, ReadableBuffer payload)
        {
            EndOfMessage = endOfMessage;
            Opcode = opcode;
            Payload = payload;
        }

        /// <summary>
        /// Creates a new <see cref="WebSocketFrame"/> containing the same information, but with all buffers
        /// copied to new heap memory.
        /// </summary>
        /// <returns></returns>
        public WebSocketFrame Copy()
        {
            return new WebSocketFrame(
                endOfMessage: EndOfMessage,
                opcode: Opcode,
                payload: ReadableBuffer.Create(Payload.ToArray()));
        }
    }
}