using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Http;

namespace WebApplication95
{
    public class WebSockets
    {
        private WebSocket _ws;
        private HttpChannel _channel;
        private TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();

        public WebSockets(HttpChannel channel)
        {
            _channel = channel;
            var ignore = StartSending();
        }

        private async Task StartSending()
        {
            await _tcs.Task;

            while (true)
            {
                var buffer = await _channel.Output.ReadAsync();

                if (buffer.IsEmpty && _channel.Output.Reading.IsCompleted)
                {
                    break;
                }

                foreach (var memory in buffer)
                {
                    ArraySegment<byte> data;
                    if (memory.TryGetArray(out data))
                    {
                        await _ws.SendAsync(data, WebSocketMessageType.Text, endOfMessage: true, cancellationToken: CancellationToken.None);
                    }
                }

                _channel.Output.Advance(buffer.End);
            }

            _channel.Output.CompleteReader();
        }

        public async Task ProcessRequest(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                await Task.CompletedTask;
                return;
            }

            var ws = await context.WebSockets.AcceptWebSocketAsync();

            _ws = ws;

            _tcs.TrySetResult(null);

            var buffer = new byte[2048];
            while (true)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                // TODO: Fragments
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    await _channel.Input.WriteAsync(new Span<byte>(buffer, 0, result.Count));
                }
                else if (result.MessageType == WebSocketMessageType.Binary)
                {
                    await _channel.Input.WriteAsync(new Span<byte>(buffer, 0, result.Count));
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    // TODO: needs to remove itself from connection mamanger?
                    break;
                }
            }
        }
    }
}
