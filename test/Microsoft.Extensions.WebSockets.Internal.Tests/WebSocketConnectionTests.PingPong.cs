// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Tests.Common;
using Xunit;

namespace Microsoft.Extensions.WebSockets.Internal.Tests
{
    public partial class WebSocketConnectionTests
    {
        [Fact]
        public async Task AutomaticPingTransmission()
        {
            var startTime = DateTime.UtcNow;
            // Arrange
            using (var pair = WebSocketPair.Create(
                serverOptions: new WebSocketOptions().WithAllFramesPassedThrough().WithPingInterval(TimeSpan.FromMilliseconds(10)),
                clientOptions: new WebSocketOptions().WithAllFramesPassedThrough()))
            {
                var client = pair.ClientSocket.ExecuteAndCaptureFramesAsync();
                var server = pair.ServerSocket.ExecuteAndCaptureFramesAsync();

                // Act
                // Wait for pings to be sent
                await Task.Delay(500);

                await pair.ServerSocket.CloseAsync(WebSocketCloseStatus.NormalClosure).OrTimeout();
                var clientSummary = await client.OrTimeout();
                await pair.ClientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure).OrTimeout();
                var serverSummary = await server.OrTimeout();

                // Assert
                Assert.NotEqual(0, clientSummary.Received.Count);

                Assert.True(clientSummary.Received.All(f => f.EndOfMessage));
                Assert.True(clientSummary.Received.All(f => f.Opcode == WebSocketOpcode.Ping));
                Assert.True(clientSummary.Received.All(f =>
                {
                    var str = Encoding.UTF8.GetString(f.Payload.ToArray());

                    // We can't verify the exact timestamp, but we can verify that it is a timestamp created after we started.
                    if (DateTime.TryParseExact(str, "O", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var dt))
                    {
                        return dt >= startTime;
                    }
                    return false;
                }));
            }
        }

        [Fact]
        public async Task AutomaticPingResponse()
        {
            // Arrange
            using (var pair = WebSocketPair.Create(
                serverOptions: new WebSocketOptions().WithAllFramesPassedThrough(),
                clientOptions: new WebSocketOptions().WithAllFramesPassedThrough()))
            {
                var payload = Encoding.UTF8.GetBytes("ping payload");

                var pongTcs = new TaskCompletionSource<WebSocketFrame>();

                var client = pair.ClientSocket.ExecuteAsync(f =>
                {
                    if (f.Opcode == WebSocketOpcode.Pong)
                    {
                        pongTcs.TrySetResult(f.Copy());
                    }
                    else
                    {
                        Assert.False(true, "Received non-pong frame from server!");
                    }
                });
                var server = pair.ServerSocket.ExecuteAndCaptureFramesAsync();

                // Act
                await pair.ClientSocket.SendAsync(new WebSocketFrame(
                    endOfMessage: true,
                    opcode: WebSocketOpcode.Ping,
                    payload: ReadableBuffer.Create(payload)));

                var pongFrame = await pongTcs.Task.OrTimeout();

                await pair.ClientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure).OrTimeout();
                await server.OrTimeout();
                await pair.ServerSocket.CloseAsync(WebSocketCloseStatus.NormalClosure).OrTimeout();
                await client.OrTimeout();

                // Assert
                Assert.True(pongFrame.EndOfMessage);
                Assert.Equal(WebSocketOpcode.Pong, pongFrame.Opcode);
                Assert.Equal(payload, pongFrame.Payload.ToArray());
            }
        }
    }
}
