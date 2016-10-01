using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Sockets
{
    public class WebSockets : IHttpTransport
    {
        private readonly HttpChannel _channel;
        private readonly Connection _connection;
        private readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();
        private WebSocket _ws;

        public WebSockets(Connection connection)
        {
            _connection = connection;
            _channel = (HttpChannel)connection.Channel;
            var ignore = StartSending();
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

            while (!_channel.Input.Writing.IsCompleted)
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
                    await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    // TODO: needs to remove itself from connection mamanger?
                    break;
                }
            }
        }

        public async Task CloseAsync()
        {
            await _tcs.Task;

            // REVIEW: Close output vs Close?
            await _ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
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
    }
}
