using System;
using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Sockets
{
    public class LongPolling : IHttpTransport
    {
        private readonly TaskCompletionSource<object> _initTcs = new TaskCompletionSource<object>();
        private readonly TaskCompletionSource<object> _lifetime = new TaskCompletionSource<object>();
        private readonly HttpChannel _channel;
        private readonly Connection _connection;
        private readonly TaskQueue _queue;

        private HttpContext _context;

        public LongPolling(Connection connection)
        {
            _queue = new TaskQueue(_initTcs.Task);
            _connection = connection;
            _channel = (HttpChannel)connection.Channel;
        }

        public async Task ProcessRequest(HttpContext context)
        {
            _context = context;

            _initTcs.TrySetResult(null);

            // Send queue messages to the connection
            var ignore = ProcessMessages(context);

            await _lifetime.Task;
        }

        private async Task ProcessMessages(HttpContext context)
        {
            var buffer = await _channel.Output.ReadAsync();

            if (buffer.IsEmpty && _channel.Output.Reading.IsCompleted)
            {
                await CloseAsync();
                return;
            }

            try
            {
                await Send(buffer);
            }
            finally
            {
                _channel.Output.Advance(buffer.End);
            }

            await EndRequest();
        }

        public async Task CloseAsync()
        {
            await _queue.Enqueue(state =>
            {
                var context = (HttpContext)state;
                // REVIEW: What happens if header was already?
                context.Response.Headers["X-ASPNET-SOCKET-DISCONNECT"] = "1";
                return Task.CompletedTask;
            },
            _context);

            await EndRequest();
        }

        private async Task EndRequest()
        {
            // Drain the queue and don't let any new work enter
            await _queue.Drain();

            // Complete the lifetime task
            _lifetime.TrySetResult(null);
        }

        private Task Send(ReadableBuffer value)
        {
            // REVIEW: Can we avoid the closure here?
            return _queue.Enqueue(state =>
            {
                var data = (ReadableBuffer)state;
                _context.Response.ContentLength = data.Length;
                return data.CopyToAsync(_context.Response.Body);
            },
            value);
        }
    }
}
