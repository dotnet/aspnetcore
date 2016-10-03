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

            var outputBuffer = _channel.Input.Alloc();

            while (!_channel.Input.Writing.IsCompleted)
            {
                // Make sure there's room to read (at least 2k)
                outputBuffer.Ensure(2048);

                ArraySegment<byte> segment;
                if (!outputBuffer.Memory.TryGetArray(out segment))
                {
                    // REVIEW: Do we care about native buffers here?
                    throw new InvalidOperationException("Managed buffers are required for Web Socket API");
                }

                var result = await ws.ReceiveAsync(segment, CancellationToken.None);

                if (result.MessageType != WebSocketMessageType.Close)
                {
                    outputBuffer.Advance(result.Count);

                    if (result.EndOfMessage)
                    {
                        // Flush when we get an entire message
                        await outputBuffer.FlushAsync();

                        // Allocate a new buffer to further writing
                        outputBuffer = _channel.Input.Alloc();
                    }
                }
                else
                {
                    await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
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
