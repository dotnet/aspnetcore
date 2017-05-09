using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    internal class TestConnection : IConnection
    {
        private TaskCompletionSource<object> _started = new TaskCompletionSource<object>();
        private TaskCompletionSource<object> _disposed = new TaskCompletionSource<object>();

        private Channel<Message> _sentMessages = Channel.CreateUnbounded<Message>();
        private Channel<Message> _receivedMessages = Channel.CreateUnbounded<Message>();

        private CancellationTokenSource _receiveShutdownToken = new CancellationTokenSource();
        private Task _receiveLoop;

        public event Action Connected;
        public event Action<byte[], MessageType> Received;
        public event Action<Exception> Closed;

        public Task Started => _started.Task;
        public Task Disposed => _disposed.Task;
        public ReadableChannel<Message> SentMessages => _sentMessages.In;
        public WritableChannel<Message> ReceivedMessages => _receivedMessages.Out;

        public TestConnection()
        {
            _receiveLoop = ReceiveLoopAsync(_receiveShutdownToken.Token);
        }

        public Task DisposeAsync()
        {
            _disposed.TrySetResult(null);
            _receiveShutdownToken.Cancel();
            return _receiveLoop;
        }

        public async Task SendAsync(byte[] data, MessageType type, CancellationToken cancellationToken)
        {
            if(!_started.Task.IsCompleted)
            {
                throw new InvalidOperationException("Connection must be started before SendAsync can be called");
            }

            var message = new Message(data, type, endOfMessage: true);
            while (await _sentMessages.Out.WaitToWriteAsync(cancellationToken))
            {
                if (_sentMessages.Out.TryWrite(message))
                {
                    return;
                }
            }
            throw new ObjectDisposedException("Unable to send message, underlying channel was closed");
        }

        public Task StartAsync(ITransportFactory transportFactory, HttpClient httpClient)
        {
            _started.TrySetResult(null);
            Connected?.Invoke();
            return Task.CompletedTask;
        }

        public async Task<string> ReadSentTextMessageAsync()
        {
            var message = await SentMessages.ReadAsync();
            if (message.Type != MessageType.Text)
            {
                throw new InvalidOperationException($"Unexpected message of type: {message.Type}");
            }
            return Encoding.UTF8.GetString(message.Payload);
        }

        public Task ReceiveJsonMessage(object jsonObject)
        {
            var json = JsonConvert.SerializeObject(jsonObject, Formatting.None);
            var bytes = Encoding.UTF8.GetBytes(json);
            var message = new Message(bytes, MessageType.Text);

            return _receivedMessages.Out.WriteAsync(message);
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    while (await _receivedMessages.In.WaitToReadAsync(token))
                    {
                        while (_receivedMessages.In.TryRead(out var message))
                        {
                            Received?.Invoke(message.Payload, message.Type);
                        }
                    }
                }
                Closed?.Invoke(null);
            }
            catch (OperationCanceledException)
            {
                // Do nothing, we were just asked to shut down.
                Closed?.Invoke(null);
            }
            catch (Exception ex)
            {
                Closed?.Invoke(ex);
            }
        }
    }
}