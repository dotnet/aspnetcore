using System;
using System.Text;
using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Http;

namespace WebApplication95
{
    public class LongPolling
    {
        private Task _lastTask;
        private object _lockObj = new object();
        private bool _completed;
        private TaskCompletionSource<object> _initTcs = new TaskCompletionSource<object>();
        private TaskCompletionSource<object> _lifetime = new TaskCompletionSource<object>();
        private HttpContext _context;
        private readonly ConnectionState _state;

        public LongPolling(ConnectionState state)
        {
            _lastTask = _initTcs.Task;
            _state = state;
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
            var buffer = await _state.Connection.Output.ReadAsync();

            foreach (var memory in buffer)
            {
                ArraySegment<byte> data;
                if (memory.TryGetArray(out data))
                {
                    await Send(data);

                    // Advance the buffer one block of memory
                    buffer = buffer.Slice(memory.Length);
                    _state.Connection.Output.Advance(buffer.Start);
                    break;
                }
            }
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

        public async Task Send(ArraySegment<byte> value)
        {
            await Post(async state =>
            {
                var data = ((ArraySegment<byte>)state);
                _context.Response.Headers["X-SignalR-ConnectionId"] = _state.Connection.ConnectionId;
                _context.Response.ContentLength = data.Count;
                await _context.Response.Body.WriteAsync(data.Array, data.Offset, data.Count);
            },
            value);

            CompleteRequest();
        }
    }
}
