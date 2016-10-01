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
        private readonly TaskQueue _queue;

        private HttpContext _context;

        public LongPolling(HttpChannel channel)
        {
            _queue = new TaskQueue(_initTcs.Task);
            _channel = channel;
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
                Abort();
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


            Abort();
        }

        public async void Abort()
        {
            // Drain the queue and don't let any new work enter
            await _queue.Drain();

            // Complete the lifetime task
            _lifetime.TrySetResult(null);
        }

        private Task Send(ReadableBuffer value)
        {
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
