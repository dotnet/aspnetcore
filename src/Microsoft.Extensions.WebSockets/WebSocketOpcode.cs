namespace Microsoft.Extensions.WebSockets
{
    /// <summary>
    /// Represents the possible values for the "opcode" field of a WebSocket frame.
    /// </summary>
    public enum WebSocketOpcode
    {
        /// <summary>
        /// Indicates that the frame is a continuation of the previous <see cref="Text"/> or <see cref="Binary"/> frame.
        /// </summary>
        Continuation = 0x0,

        /// <summary>
        /// Indicates that the frame is the first frame of a new Text message, formatted in UTF-8.
        /// </summary>
        Text = 0x1,

        /// <summary>
        /// Indicates that the frame is the first frame of a new Binary message.
        /// </summary>
        Binary = 0x2,
        /* 0x3 - 0x7 are reserved */

        /// <summary>
        /// Indicates that the frame is a notification that the sender is closing their end of the connection
        /// </summary>
        Close = 0x8,

        /// <summary>
        /// Indicates a request from the sender to receive a <see cref="Pong"/>, in order to maintain the connection.
        /// </summary>
        Ping = 0x9,

        /// <summary>
        /// Indicates a response to a <see cref="Ping"/>, in order to maintain the connection.
        /// </summary>
        Pong = 0xA,
        /* 0xB-0xF are reserved */

        /* all opcodes above 0xF are invalid */
    }
}