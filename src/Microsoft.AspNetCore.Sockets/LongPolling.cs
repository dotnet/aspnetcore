using System;
using System.Text;
using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Sockets
{
    public class LongPolling
    {
        private Task _lastTask;
        private object _lockObj = new object();
        private bool _completed;
        private TaskCompletionSource<object> _initTcs = new TaskCompletionSource<object>();
        private TaskCompletionSource<object> _lifetime = new TaskCompletionSource<object>();
        private HttpContext _context;
        private readonly HttpChannel _channel;

        public LongPolling(HttpChannel channel)
        {
            _lastTask = _initTcs.Task;
            _channel = channel;
        }

        private Task Post(Func<object, Task> work, object state)
        {
            if (_completed)
            {
                return _lastTask;
            }

            lock (_lockObj)
            {
                _lastTask = _lastTask.ContinueWith((t, s1) => work(s1), state).Unwrap();
            }

            return _lastTask;
        }

        public async Task ProcessRequest(HttpContext context)
        {
            // End the connection if the client goes away
            context.RequestAborted.Register(state => OnConnectionAborted(state), this);

            _context = context;

            _initTcs.TrySetResult(null);

            // Send queue messages to the connection
            var ignore = ProcessMessages(context);

            await _lifetime.Task;

            _completed = true;
        }

        private async Task ProcessMessages(HttpContext context)
        {
            var buffer = await _channel.Output.ReadAsync();

            if (buffer.IsEmpty && _channel.Output.Reading.IsCompleted)
            {
                CompleteRequest();
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


            CompleteRequest();
        }

        private static void OnConnectionAborted(object state)
        {
            ((LongPolling)state).CompleteRequest();
        }

        private void CompleteRequest()
        {
            Post(state =>
            {
                ((TaskCompletionSource<object>)state).TrySetResult(null);
                return Task.CompletedTask;
            },
            _lifetime);
        }

        public Task Send(ReadableBuffer value)
        {
            return Post(async state =>
            {
                var data = ((ReadableBuffer)state);
                _context.Response.ContentLength = data.Length;
                foreach (var memory in data)
                {
                    ArraySegment<byte> segment;
                    if (memory.TryGetArray(out segment))
                    {
                        await _context.Response.Body.WriteAsync(segment.Array, segment.Offset, segment.Count);
                    }
                }
            },
            value);
        }
    }
}
