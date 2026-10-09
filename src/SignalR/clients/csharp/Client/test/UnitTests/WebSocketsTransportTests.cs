// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;

public class WebSocketsTransportTests : VerifiableLoggedTest
{
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task LogsContentOnlyWhenEnabledWithoutChangingPayload(bool enabled, bool traceEnabled)
    {
        var payload = Encoding.UTF8.GetBytes("MALFORMED_44383_PAYLOAD_MARKER{");
        var socket = new ContentWebSocket(payload);
        var sink = new TestSink();
        using var loggerFactory = new TestLoggerFactory(sink, traceEnabled);
        var options = new HttpConnectionOptions
        {
            WebSocketFactory = (context, token) => ValueTask.FromResult<WebSocket>(socket),
            CloseTimeout = TimeSpan.FromMilliseconds(50),
            LogMessageContent = enabled,
        };
        var transport = new WebSocketsTransport(options, loggerFactory, () => Task.FromResult<string>(null), null);
        try
        {
            await transport.StartAsync(new Uri("http://example.com"), TransferFormat.Text).DefaultTimeout();
            var result = await transport.Input.ReadAsync().AsTask().DefaultTimeout();
            Assert.Equal(payload, result.Buffer.ToArray());
            transport.Input.AdvanceTo(result.Buffer.End);
            await transport.Output.WriteAsync(payload);
            Assert.Equal(payload, await socket.Sent.Task.DefaultTimeout());
            var contentLogs = sink.Writes.Where(write => write.EventId.Name is "ReceivedMessageContent" or "SendingMessageContent").ToArray();
            Assert.Equal(enabled && traceEnabled ? 2 : 0, contentLogs.Length);
            if (enabled && traceEnabled)
            {
                payload.AsSpan().Fill((byte)'x');
                Assert.Contains(contentLogs, write => write.EventId.Name == "ReceivedMessageContent");
                Assert.Contains(contentLogs, write => write.EventId.Name == "SendingMessageContent");
                Assert.All(contentLogs, write => Assert.Contains("MALFORMED_44383_PAYLOAD_MARKER{", write.Message));
            }
        }
        finally
        {
            await transport.StopAsync().DefaultTimeout();
        }
    }

    private sealed class ContentWebSocket(byte[] payload) : TestWebSocket
    {
        private bool _received;
        public TaskCompletionSource<byte[]> Sent { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (buffer.IsEmpty)
            {
                if (_received)
                {
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                }
                return new ValueWebSocketReceiveResult(0, WebSocketMessageType.Text, false);
            }
            payload.CopyTo(buffer);
            _received = true;
            return new ValueWebSocketReceiveResult(payload.Length, WebSocketMessageType.Text, true);
        }

        public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken) => Task.CompletedTask;

        public override ValueTask SendAsync(ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            Sent.TrySetResult(buffer.ToArray());
            return default;
        }
    }

    // Tests that the transport can still be stopped if SendAsync and ReceiveAsync are hanging (ethernet unplugged for example)
    [Fact]
    public async Task StopCancelsSendAndReceive()
    {
        var options = new HttpConnectionOptions()
        {
            WebSocketFactory = (context, token) =>
            {
                return ValueTask.FromResult((WebSocket)new TestWebSocket());
            },
            CloseTimeout = TimeSpan.FromMilliseconds(1),
        };

        using (StartVerifiableLog())
        {
            var webSocketsTransport = new WebSocketsTransport(options, loggerFactory: LoggerFactory, () => Task.FromResult<string>(null), null);

            await webSocketsTransport.StartAsync(
                new Uri("http://fakeuri.org"), TransferFormat.Text).DefaultTimeout();

            await webSocketsTransport.StopAsync().DefaultTimeout();

            await webSocketsTransport.Running.DefaultTimeout();
        }
    }

    internal class TestWebSocket : WebSocket
    {
        public Task ConnectAsync(Uri uri, CancellationToken cancellationToken) => Task.CompletedTask;

        public override WebSocketCloseStatus? CloseStatus => null;

        public override string CloseStatusDescription => string.Empty;

        public override WebSocketState State => WebSocketState.Open;

        public override string SubProtocol => string.Empty;

        public override void Abort() { }

        public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
            => Task.CompletedTask;

        public override async Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
        {
            await cancellationToken.WaitForCancellationAsync();
            cancellationToken.ThrowIfCancellationRequested();
        }

        public override void Dispose() { }

        public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            await cancellationToken.WaitForCancellationAsync();
            cancellationToken.ThrowIfCancellationRequested();
            return new WebSocketReceiveResult(0, WebSocketMessageType.Text, true);
        }

        public override async Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            await cancellationToken.WaitForCancellationAsync();
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
