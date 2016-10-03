using System.Binary;
using System.Text;
using Channels;
using Channels.Text.Primitives;

namespace Microsoft.Extensions.WebSockets
{
    /// <summary>
    /// Represents the payload of a Close frame (i.e. a <see cref="WebSocketFrame"/> with an <see cref="WebSocketFrame.Opcode"/> of <see cref="WebSocketOpcode.Close"/>).
    /// </summary>
    public struct WebSocketCloseResult
    {
        internal static WebSocketCloseResult AbnormalClosure = new WebSocketCloseResult(WebSocketCloseStatus.AbnormalClosure, "Underlying transport connection was terminated");
        internal static WebSocketCloseResult Empty = new WebSocketCloseResult(WebSocketCloseStatus.Empty);

        /// <summary>
        /// Gets the close status code specified in the frame.
        /// </summary>
        public WebSocketCloseStatus Status { get; }

        /// <summary>
        /// Gets the close status description specified in the frame.
        /// </summary>
        public string Description { get; }

        public WebSocketCloseResult(WebSocketCloseStatus status) : this(status, string.Empty) { }
        public WebSocketCloseResult(WebSocketCloseStatus status, string description)
        {
            Status = status;
            Description = description;
        }

        public int GetSize() => Encoding.UTF8.GetByteCount(Description) + sizeof(ushort);

        public static bool TryParse(ReadableBuffer payload, out WebSocketCloseResult result)
        {
            if(payload.Length == 0)
            {
                // Empty payload is OK
                result = new WebSocketCloseResult(WebSocketCloseStatus.Empty, string.Empty);
                return true;
            }
            else if(payload.Length < 2)
            {
                result = default(WebSocketCloseResult);
                return false;
            }
            else
            {
                var status = payload.ReadBigEndian<ushort>();
                var description = string.Empty;
                payload = payload.Slice(2);
                if(payload.Length > 0)
                {
                    description = payload.GetUtf8String();
                }
                result = new WebSocketCloseResult((WebSocketCloseStatus)status, description);
                return true;
            }
        }

        public void WriteTo(ref WritableBuffer buffer)
        {
            buffer.WriteBigEndian((ushort)Status);
            if (!string.IsNullOrEmpty(Description))
            {
                buffer.WriteUtf8String(Description);
            }
        }
    }
}