// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Formatters;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    internal class TestConnection : IConnection
    {
        private readonly bool _autoHandshake;
        private readonly TaskCompletionSource<object> _started = new TaskCompletionSource<object>();
        private readonly TaskCompletionSource<object> _disposed = new TaskCompletionSource<object>();

        private int _disposeCount = 0;

        public Task Started => _started.Task;
        public Task Disposed => _disposed.Task;

        private readonly Func<Task> _onStart;
        private readonly Func<Task> _onDispose;

        public IDuplexPipe Application { get; }
        public IDuplexPipe Transport { get; }

        public IFeatureCollection Features { get; } = new FeatureCollection();
        public int DisposeCount => _disposeCount;

        public TestConnection(Func<Task> onStart = null, Func<Task> onDispose = null, bool autoHandshake = true, bool synchronousCallbacks = false)
        {
            _autoHandshake = autoHandshake;
            _onStart = onStart ?? (() => Task.CompletedTask);
            _onDispose = onDispose ?? (() => Task.CompletedTask);

            var scheduler = synchronousCallbacks ? PipeScheduler.Inline : null;
            var options = new PipeOptions(readerScheduler: scheduler, writerScheduler: scheduler, useSynchronizationContext: false);

            var pair = DuplexPipe.CreateConnectionPair(options, options);
            Application = pair.Application;
            Transport = pair.Transport;

            Application.Input.OnWriterCompleted((ex, _) => Application.Output.Complete(), null);
        }

        public Task DisposeAsync() => DisposeCoreAsync();

        public Task StartAsync() => StartAsync(TransferFormat.Binary);

        public async Task StartAsync(TransferFormat transferFormat)
        {
            _started.TrySetResult(null);

            await _onStart();

            if (_autoHandshake)
            {
                // We can't await this as it will block StartAsync which will block
                // HubConnection.StartAsync which sends the Handshake in the first place!
                _ = ReadHandshakeAndSendResponseAsync();
            }
        }

        public async Task<string> ReadHandshakeAndSendResponseAsync()
        {
            var s = await ReadSentTextMessageAsync();

            byte[] response;

            var output = MemoryBufferWriter.Get();
            try
            {
                HandshakeProtocol.WriteResponseMessage(HandshakeResponseMessage.Empty, output);
                response = output.ToArray();
            }
            finally
            {
                MemoryBufferWriter.Return(output);
            }

            await Application.Output.WriteAsync(response);

            return s;
        }

        public Task ReceiveJsonMessage(object jsonObject)
        {
            var json = JsonConvert.SerializeObject(jsonObject, Formatting.None);
            var bytes = FormatMessageToArray(Encoding.UTF8.GetBytes(json));

            return Application.Output.WriteAsync(bytes).AsTask();
        }

        public Task ReceiveTextAsync(string rawText)
        {
            return ReceiveBytesAsync(Encoding.UTF8.GetBytes(rawText));
        }

        public Task ReceiveBytesAsync(byte[] bytes)
        {
            return Application.Output.WriteAsync(bytes).AsTask();
        }

        public async Task<string> ReadSentTextMessageAsync()
        {
            // Read a single text message from the Application Input pipe
            while (true)
            {
                var result = await Application.Input.ReadAsync();
                var buffer = result.Buffer;
                var consumed = buffer.Start;

                try
                {
                    if (TextMessageParser.TryParseMessage(ref buffer, out var payload))
                    {
                        consumed = buffer.Start;
                        return Encoding.UTF8.GetString(payload.ToArray());
                    }
                    else if (result.IsCompleted)
                    {
                        throw new InvalidOperationException("Out of data!");
                    }
                }
                finally
                {
                    Application.Input.AdvanceTo(consumed);
                }
            }
        }

        public void CompleteFromTransport(Exception ex = null)
        {
            Application.Output.Complete(ex);
        }

        private async Task DisposeCoreAsync(Exception ex = null)
        {
            Interlocked.Increment(ref _disposeCount);
            _disposed.TrySetResult(null);
            await _onDispose();

            // Simulate HttpConnection's behavior by Completing the Transport pipe.
            Transport.Input.Complete();
            Transport.Output.Complete();
        }

        private byte[] FormatMessageToArray(byte[] message)
        {
            var output = new MemoryStream();
            output.Write(message, 0, message.Length);
            TextMessageFormatter.WriteRecordSeparator(output);
            return output.ToArray();
        }
    }
}

