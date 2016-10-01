using System;
using System.Threading.Tasks;
using Channels;
using Microsoft.AspNetCore.Http;

namespace WebApplication95
{
    public class ServerSentEvents
    {
        private Task _lastTask;
        private object _lockObj = new object();
        private bool _completed;
        private TaskCompletionSource<object> _initTcs = new TaskCompletionSource<object>();
        private TaskCompletionSource<object> _lifetime = new TaskCompletionSource<object>();
        private HttpContext _context;
        private readonly HttpChannel _channel;

        public ServerSentEvents(HttpChannel channel)
        {
            _channel = channel;
            _lastTask = _initTcs.Task;
            var ignore = StartSending();
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
            context.Response.ContentType = "text/event-stream";
            context.Response.Headers["Cache-Control"] = "no-cache";

            // End the connection if the client goes away
            context.RequestAborted.Register(state => OnConnectionAborted(state), this);
            _context = context;

            // Set the initial TCS when everything is setup
            _initTcs.TrySetResult(null);

            await _lifetime.Task;

            _completed = true;
        }

        private static void OnConnectionAborted(object state)
        {
            ((ServerSentEvents)state).OnConnectedAborted();
        }

        private void OnConnectedAborted()
        {
            Post(state =>
            {
                ((TaskCompletionSource<object>)state).TrySetResult(null);
                return Task.CompletedTask;
            },
            _lifetime);
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
            return Post(async state =>
            {
                var data = ((ReadableBuffer)state);
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
                data.CopyTo(new Span<byte>(buffer, at, buffer.Length - at));
                at += data.Length;
                buffer[at++] = (byte)'\n';
                buffer[at++] = (byte)'\n';
                await _context.Response.Body.WriteAsync(buffer, 0, at);
            },
            value);
        }
    }
}
