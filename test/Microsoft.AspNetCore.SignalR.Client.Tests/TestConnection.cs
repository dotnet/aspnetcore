// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Protocols;
using Microsoft.AspNetCore.SignalR.Internal.Formatters;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets.Client;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    internal class TestConnection : IConnection
    {
        private TaskCompletionSource<object> _started = new TaskCompletionSource<object>();
        private TaskCompletionSource<object> _disposed = new TaskCompletionSource<object>();

        private Channel<byte[]> _sentMessages = Channel.CreateUnbounded<byte[]>();
        private Channel<byte[]> _receivedMessages = Channel.CreateUnbounded<byte[]>();

        private CancellationTokenSource _receiveShutdownToken = new CancellationTokenSource();
        private Task _receiveLoop;

        public event Action<Exception> Closed;
        public Task Started => _started.Task;
        public Task Disposed => _disposed.Task;
        public ChannelReader<byte[]> SentMessages => _sentMessages.Reader;
        public ChannelWriter<byte[]> ReceivedMessages => _receivedMessages.Writer;

        private bool _closed;
        private object _closedLock = new object();

        public List<ReceiveCallback> Callbacks { get; } = new List<ReceiveCallback>();

        public IFeatureCollection Features { get; } = new FeatureCollection();

        public TestConnection()
        {
            _receiveLoop = ReceiveLoopAsync(_receiveShutdownToken.Token);
        }

        public Task AbortAsync(Exception ex) => DisposeCoreAsync(ex);
        public Task DisposeAsync() => DisposeCoreAsync();

        // TestConnection isn't restartable
        public Task StopAsync() => DisposeAsync();

        private Task DisposeCoreAsync(Exception ex = null)
        {
            TriggerClosed(ex);
            _receiveShutdownToken.Cancel();
            return _receiveLoop;
        }

        public async Task SendAsync(byte[] data, CancellationToken cancellationToken)
        {
            if (!_started.Task.IsCompleted)
            {
                throw new InvalidOperationException("Connection must be started before SendAsync can be called");
            }

            while (await _sentMessages.Writer.WaitToWriteAsync(cancellationToken))
            {
                if (_sentMessages.Writer.TryWrite(data))
                {
                    return;
                }
            }
            throw new ObjectDisposedException("Unable to send message, underlying channel was closed");
        }

        public Task StartAsync(TransferFormat transferFormat)
        {
            _started.TrySetResult(null);
            return Task.CompletedTask;
        }

        public async Task ReadHandshakeAndSendResponseAsync()
        {
            await SentMessages.ReadAsync();

            var output = new MemoryStream();
            HandshakeProtocol.WriteResponseMessage(HandshakeResponseMessage.Empty, output);

            await _receivedMessages.Writer.WriteAsync(output.ToArray());
        }

        public async Task<string> ReadSentTextMessageAsync()
        {
            var message = await SentMessages.ReadAsync();
            return Encoding.UTF8.GetString(message);
        }

        public Task ReceiveJsonMessage(object jsonObject)
        {
            var json = JsonConvert.SerializeObject(jsonObject, Formatting.None);
            var bytes = FormatMessageToArray(Encoding.UTF8.GetBytes(json));

            return _receivedMessages.Writer.WriteAsync(bytes).AsTask();
        }

        private byte[] FormatMessageToArray(byte[] message)
        {
            var output = new MemoryStream();
            output.Write(message, 0, message.Length);
            TextMessageFormatter.WriteRecordSeparator(output);
            return output.ToArray();
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    while (await _receivedMessages.Reader.WaitToReadAsync(token))
                    {
                        while (_receivedMessages.Reader.TryRead(out var message))
                        {
                            ReceiveCallback[] callbackCopies;
                            lock (Callbacks)
                            {
                                callbackCopies = Callbacks.ToArray();
                            }

                            foreach (var callback in callbackCopies)
                            {
                                await callback.InvokeAsync(message);
                            }
                        }
                    }
                }
                TriggerClosed();
            }
            catch (OperationCanceledException)
            {
                // Do nothing, we were just asked to shut down.
                TriggerClosed();
            }
            catch (Exception ex)
            {
                TriggerClosed(ex);
            }
        }

        private void TriggerClosed(Exception ex = null)
        {
            lock (_closedLock)
            {
                if (!_closed)
                {
                    _closed = true;
                    Closed?.Invoke(ex);
                }
            }
        }

        public IDisposable OnReceived(Func<byte[], object, Task> callback, object state)
        {
            var receiveCallBack = new ReceiveCallback(callback, state);
            lock (Callbacks)
            {
                Callbacks.Add(receiveCallBack);
            }
            return new Subscription(receiveCallBack, Callbacks);
        }

        public class ReceiveCallback
        {
            private readonly Func<byte[], object, Task> _callback;
            private readonly object _state;

            public ReceiveCallback(Func<byte[], object, Task> callback, object state)
            {
                _callback = callback;
                _state = state;
            }

            public Task InvokeAsync(byte[] data)
            {
                return _callback(data, _state);
            }
        }

        private class Subscription : IDisposable
        {
            private readonly ReceiveCallback _callback;
            private readonly List<ReceiveCallback> _callbacks;
            public Subscription(ReceiveCallback callback, List<ReceiveCallback> callbacks)
            {
                _callback = callback;
                _callbacks = callbacks;
            }

            public void Dispose()
            {
                lock (_callbacks)
                {
                    _callbacks.Remove(_callback);
                }
            }
        }
    }
}
