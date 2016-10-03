using System.Collections.Generic;
using System.Threading.Tasks;
using Channels;

namespace Microsoft.Extensions.WebSockets.Tests
{
    public static class WebSocketConnectionExtensions
    {
        public static async Task<WebSocketConnectionSummary> ExecuteAndCaptureFramesAsync(this IWebSocketConnection self)
        {
            var frames = new List<WebSocketFrame>();
            var closeResult = await self.ExecuteAsync(frame =>
            {
                var buffer = new byte[frame.Payload.Length];
                frame.Payload.CopyTo(buffer);
                frames.Add(new WebSocketFrame(
                    frame.EndOfMessage,
                    frame.Opcode,
                    ReadableBuffer.Create(buffer, 0, buffer.Length)));
            });
            return new WebSocketConnectionSummary(frames, closeResult);
        }
    }
}
