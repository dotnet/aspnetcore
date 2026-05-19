// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.WebSockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;

public class WebSocketsTransportTests : VerifiableLoggedTest
{
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
