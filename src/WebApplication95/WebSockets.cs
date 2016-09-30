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
        private ConnectionState _state;
        private TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();

        public WebSockets(ConnectionState state)
        {
            _state = state;
            var ignore = StartSending();
        }

        private async Task StartSending()
        {
            await _tcs.Task;

            while (true)
            {
                var buffer = await _state.Connection.Output.ReadAsync();

                if (buffer.IsEmpty && _state.Connection.Output.Reading.IsCompleted)
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

                _state.Connection.Output.Advance(buffer.End);
            }

            _state.Connection.Output.CompleteReader();
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
                    await _state.Connection.Input.WriteAsync(new Span<byte>(buffer, 0, result.Count));
                }
                else if (result.MessageType == WebSocketMessageType.Binary)
                {
                    await _state.Connection.Input.WriteAsync(new Span<byte>(buffer, 0, result.Count));
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }
            }
        }
    }
}
