using System;
using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Sockets
{
    public class ServerSentEvents : IHttpTransport
    {
        private readonly TaskQueue _queue;
        private readonly TaskCompletionSource<object> _initTcs = new TaskCompletionSource<object>();
        private readonly TaskCompletionSource<object> _lifetime = new TaskCompletionSource<object>();
        private readonly HttpChannel _channel;

        private HttpContext _context;

        public ServerSentEvents(HttpChannel channel)
        {
            _queue = new TaskQueue(_initTcs.Task);
            _channel = channel;
            var ignore = StartSending();
        }

        public async Task ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/event-stream";
            context.Response.Headers["Cache-Control"] = "no-cache";

            _context = context;

            // Set the initial TCS when everything is setup
            _initTcs.TrySetResult(null);

            await _lifetime.Task;
        }

        public async void Abort()
        {
            // Drain the queue so no new work can enter
            await _queue.Drain();

            // Complete the lifetime task
            _lifetime.TrySetResult(null);
        }

        private async Task StartSending()
        {
            await _initTcs.Task;

            while (true)
            {
                var buffer = await _channel.Output.ReadAsync();

                if (buffer.IsEmpty && _channel.Output.Reading.IsCompleted)
                {
                    break;
                }

                await Send(buffer);

                _channel.Output.Advance(buffer.End);
            }

            _channel.Output.CompleteReader();
        }

        private Task Send(ReadableBuffer value)
        {
            return _queue.Enqueue(state =>
            {
                var data = (ReadableBuffer)state;
                // TODO: Pooled buffers
                // 8 = 6(data: ) + 2 (\n\n)
                var buffer = new byte[8 + data.Length];
                var at = 0;
                buffer[at++] = (byte)'d';
                buffer[at++] = (byte)'a';
                buffer[at++] = (byte)'t';
                buffer[at++] = (byte)'a';
                buffer[at++] = (byte)':';
                buffer[at++] = (byte)' ';
                data.CopyTo(new Span<byte>(buffer, at, data.Length));
                at += data.Length;
                buffer[at++] = (byte)'\n';
                buffer[at++] = (byte)'\n';
                return _context.Response.Body.WriteAsync(buffer, 0, at);
            },
            value);
        }
    }
}
