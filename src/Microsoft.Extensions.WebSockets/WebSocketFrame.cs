using System;
using System.Text;
using Channels;

namespace Microsoft.Extensions.WebSockets
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
    }
}