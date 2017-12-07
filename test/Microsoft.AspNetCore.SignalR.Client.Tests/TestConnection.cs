// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Formatters;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.AspNetCore.Sockets.Features;
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

        private TransferMode? _transferMode;
        private readonly TaskCompletionSource<object> _closeTcs = new TaskCompletionSource<object>();

        public Task Closed => _closeTcs.Task;
        public Task Started => _started.Task;
        public Task Disposed => _disposed.Task;
        public ChannelReader<byte[]> SentMessages => _sentMessages.Reader;
        public ChannelWriter<byte[]> ReceivedMessages => _receivedMessages.Writer;

        private readonly List<ReceiveCallback> _callbacks = new List<ReceiveCallback>();

        public IFeatureCollection Features { get; } = new FeatureCollection();

        public TestConnection(TransferMode? transferMode = null)
        {
            _transferMode = transferMode;
            _receiveLoop = ReceiveLoopAsync(_receiveShutdownToken.Token);
        }

        public Task AbortAsync(Exception ex) => DisposeCoreAsync(ex);
        public Task DisposeAsync() => DisposeCoreAsync();

        private Task DisposeCoreAsync(Exception ex = null)
        {
            if (ex == null)
            {
                _closeTcs.TrySetResult(null);
                _disposed.TrySetResult(null);
            }
            else
            {
                _closeTcs.TrySetException(ex);
                _disposed.TrySetException(ex);
            }

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

        public Task StartAsync()
        {
            if (_transferMode.HasValue)
            {
                var transferModeFeature = Features.Get<ITransferModeFeature>();
                if (transferModeFeature == null)
                {
                    transferModeFeature = new TransferModeFeature();
                    Features.Set(transferModeFeature);
                }

                transferModeFeature.TransferMode = _transferMode.Value;
            }

            _started.TrySetResult(null);
            return Task.CompletedTask;
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

            return _receivedMessages.Writer.WriteAsync(bytes);
        }

        private byte[] FormatMessageToArray(byte[] message)
        {
            var output = new MemoryStream();
            TextMessageFormatter.WriteMessage(message, output);
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
                            lock (_callbacks)
                            {
                                callbackCopies = _callbacks.ToArray();
                            }

                            foreach (var callback in callbackCopies)
                            {
                                await callback.InvokeAsync(message);
                            }
                        }
                    }
                }
                _closeTcs.TrySetResult(null);
            }
            catch (OperationCanceledException)
            {
                // Do nothing, we were just asked to shut down.
                _closeTcs.TrySetResult(null);
            }
            catch (Exception ex)
            {
                _closeTcs.TrySetException(ex);
            }
        }

        public IDisposable OnReceived(Func<byte[], object, Task> callback, object state)
        {
            var receiveCallBack = new ReceiveCallback(callback, state);
            lock (_callbacks)
            {
                _callbacks.Add(receiveCallBack);
            }
            return new Subscription(receiveCallBack, _callbacks);
        }

        private class ReceiveCallback
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
