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
        private readonly WebSocketMessageType _messageType;

        public WebSockets(Connection connection, Format format)
        {
            _connection = connection;
            _channel = (HttpChannel)connection.Channel;
            _messageType = format == Format.Binary ? WebSocketMessageType.Binary : WebSocketMessageType.Text;
        }

        public async Task ProcessRequest(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                await Task.CompletedTask;
                return;
            }

            var ws = await context.WebSockets.AcceptWebSocketAsync();

            // REVIEW: Should we track this task? Leaving things like this alive usually causes memory leaks :)
            // The reason we don't await this is because the channel is disposed after this loop returns
            // and the sending loop is waiting for the channel to end before doing anything
            // We could do a 2 stage shutdown but that could complicate the code...
            var sending = StartSending(ws);

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

                    // Flush the written data to the channel
                    await outputBuffer.FlushAsync();

                    // Allocate a new buffer to further writing
                    outputBuffer = _channel.Input.Alloc();
                }
                else
                {
                    break;
                }
            }

            await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        }

        private async Task StartSending(WebSocket ws)
        {
            while (true)
            {
                var result = await _channel.Output.ReadAsync();
                var buffer = result.Buffer;

                try
                {
                    if (buffer.IsEmpty && result.IsCompleted)
                    {
                        break;
                    }

                    foreach (var memory in buffer)
                    {
                        ArraySegment<byte> data;
                        if (memory.TryGetArray(out data))
                        {
                            if (IsClosedOrClosedSent(ws))
                            {
                                break;
                            }

                            await ws.SendAsync(data, _messageType, endOfMessage: true, cancellationToken: CancellationToken.None);
                        }
                    }

                }
                catch (Exception)
                {
                    // Error writing, probably closed
                    break;
                }
                finally
                {
                    _channel.Output.Advance(buffer.End);
                }
            }

            // REVIEW: Should this ever happen?
            if (!IsClosedOrClosedSent(ws))
            {
                // Close the output
                await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
        }

        private static bool IsClosedOrClosedSent(WebSocket webSocket)
        {
            var webSocketState = GetWebSocketState(webSocket);

            return webSocketState == WebSocketState.Closed ||
                   webSocketState == WebSocketState.CloseSent ||
                   webSocketState == WebSocketState.Aborted;
        }

        private static WebSocketState GetWebSocketState(WebSocket webSocket)
        {
            try
            {
                return webSocket.State;
            }
            catch (ObjectDisposedException)
            {
                return WebSocketState.Closed;
            }
        }
    }
}
