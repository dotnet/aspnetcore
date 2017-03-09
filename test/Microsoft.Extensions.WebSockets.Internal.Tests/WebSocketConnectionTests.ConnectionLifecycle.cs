// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SignalR.Tests.Common;
using Microsoft.Extensions.Internal;
using System;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.WebSockets.Internal.Tests
{
    public partial class WebSocketConnectionTests
    {
        [Fact]
        public async Task SendReceiveFrames()
        {
            using (var pair = WebSocketPair.Create())
            {
                var client = pair.ClientSocket.ExecuteAsync(_ =>
                {
                    Assert.False(true, "did not expect the client to receive any frames!");
                    return TaskCache.CompletedTask;
                });

                // Send Frames
                await pair.ClientSocket.SendAsync(CreateTextFrame("Hello")).OrTimeout();
                await pair.ClientSocket.SendAsync(CreateTextFrame("World")).OrTimeout();
                await pair.ClientSocket.SendAsync(CreateBinaryFrame(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF })).OrTimeout();
                await pair.ClientSocket.CloseAsync(new WebSocketCloseResult(WebSocketCloseStatus.NormalClosure)).OrTimeout();

                var summary = await pair.ServerSocket.ExecuteAndCaptureFramesAsync().OrTimeout();
                Assert.Equal(3, summary.Received.Count);
                Assert.Equal("Hello", Encoding.UTF8.GetString(summary.Received[0].Payload.ToArray()));
                Assert.Equal("World", Encoding.UTF8.GetString(summary.Received[1].Payload.ToArray()));
                Assert.Equal(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, summary.Received[2].Payload.ToArray());

                await pair.ServerSocket.CloseAsync(new WebSocketCloseResult(WebSocketCloseStatus.NormalClosure)).OrTimeout();
                await client.OrTimeout();
            }
        }

        [Fact]
        public async Task ExecuteReturnsWhenCloseFrameReceived()
        {
            using (var pair = WebSocketPair.Create())
            {
                var client = pair.ClientSocket.ExecuteAndCaptureFramesAsync();
                await pair.ClientSocket.CloseAsync(new WebSocketCloseResult(WebSocketCloseStatus.InvalidMessageType, "Abc")).OrTimeout();
                var serverSummary = await pair.ServerSocket.ExecuteAndCaptureFramesAsync().OrTimeout();
                await pair.ServerSocket.CloseAsync(new WebSocketCloseResult(WebSocketCloseStatus.NormalClosure, "Ok")).OrTimeout();
                var clientSummary = await client.OrTimeout();

                Assert.Equal(0, serverSummary.Received.Count);
                Assert.Equal(WebSocketCloseStatus.InvalidMessageType, serverSummary.CloseResult.Status);
                Assert.Equal("Abc", serverSummary.CloseResult.Description);

                Assert.Equal(0, clientSummary.Received.Count);
                Assert.Equal(WebSocketCloseStatus.NormalClosure, clientSummary.CloseResult.Status);
                Assert.Equal("Ok", clientSummary.CloseResult.Description);
            }
        }

        [Fact]
        public async Task AbnormalTerminationOfInboundChannelCausesExecuteToThrow()
        {
            using (var pair = WebSocketPair.Create())
            {
                var client = pair.ClientSocket.ExecuteAndCaptureFramesAsync();
                var server = pair.ServerSocket.ExecuteAndCaptureFramesAsync();
                pair.TerminateFromClient(new InvalidOperationException("It broke!"));

                await Assert.ThrowsAsync<InvalidOperationException>(() => server);
            }
        }

        [Fact]
        public async Task StateTransitions()
        {
            using (var pair = WebSocketPair.Create())
            {
                // Initial State
                Assert.Equal(WebSocketConnectionState.Created, pair.ServerSocket.State);
                Assert.Equal(WebSocketConnectionState.Created, pair.ClientSocket.State);

                // Start the sockets
                var serverReceiving = new TaskCompletionSource<object>();
                var clientReceiving = new TaskCompletionSource<object>();
                var server = pair.ServerSocket.ExecuteAsync(frame => serverReceiving.TrySetResult(null));
                var client = pair.ClientSocket.ExecuteAsync(frame => clientReceiving.TrySetResult(null));

                // Send a frame from each and verify that the state transitioned.
                // We need to do this because it's the only way to correctly wait for the state transition (which happens asynchronously in ExecuteAsync)
                await pair.ClientSocket.SendAsync(CreateTextFrame("Hello")).OrTimeout();
                await pair.ServerSocket.SendAsync(CreateTextFrame("Hello")).OrTimeout();

                await Task.WhenAll(serverReceiving.Task, clientReceiving.Task).OrTimeout();

                // Check state
                Assert.Equal(WebSocketConnectionState.Connected, pair.ServerSocket.State);
                Assert.Equal(WebSocketConnectionState.Connected, pair.ClientSocket.State);

                // Close the server socket
                await pair.ServerSocket.CloseAsync(new WebSocketCloseResult(WebSocketCloseStatus.NormalClosure)).OrTimeout();
                await client.OrTimeout();

                // Check state
                Assert.Equal(WebSocketConnectionState.CloseSent, pair.ServerSocket.State);
                Assert.Equal(WebSocketConnectionState.CloseReceived, pair.ClientSocket.State);

                // Close the client socket
                await pair.ClientSocket.CloseAsync(new WebSocketCloseResult(WebSocketCloseStatus.NormalClosure)).OrTimeout();
                await server.OrTimeout();

                // Check state
                Assert.Equal(WebSocketConnectionState.Closed, pair.ServerSocket.State);
                Assert.Equal(WebSocketConnectionState.Closed, pair.ClientSocket.State);

                // Verify we can't restart the connection or send a message
                await Assert.ThrowsAsync<ObjectDisposedException>(async () => await pair.ServerSocket.ExecuteAsync(f => { }));
                await Assert.ThrowsAsync<ObjectDisposedException>(async () => await pair.ClientSocket.SendAsync(CreateTextFrame("Nope")));
            }
        }

        [Fact]
        public async Task CanReceiveControlFrameInTheMiddleOfFragmentedMessage()
        {
            using (var pair = WebSocketPair.Create())
            {
                // Start the sockets
                var client = pair.ClientSocket.ExecuteAndCaptureFramesAsync();
                var server = pair.ServerSocket.ExecuteAndCaptureFramesAsync();

                // Send (Fin=false, "Hello"), (Ping), (Fin=true, "World")
                await pair.ClientSocket.SendAsync(new WebSocketFrame(
                    endOfMessage: false,
                    opcode: WebSocketOpcode.Text,
                    payload: ReadableBuffer.Create(Encoding.UTF8.GetBytes("Hello"))));
                await pair.ClientSocket.SendAsync(new WebSocketFrame(
                    endOfMessage: true,
                    opcode: WebSocketOpcode.Ping,
                    payload: ReadableBuffer.Create(Encoding.UTF8.GetBytes("ping"))));
                await pair.ClientSocket.SendAsync(new WebSocketFrame(
                    endOfMessage: true,
                    opcode: WebSocketOpcode.Continuation,
                    payload: ReadableBuffer.Create(Encoding.UTF8.GetBytes("World"))));

                // Close the socket
                await pair.ClientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure);
                var serverSummary = await server;
                await pair.ServerSocket.CloseAsync(WebSocketCloseStatus.NormalClosure);
                var clientSummary = await client;

                // Assert
                var nonControlFrames = serverSummary.Received.Where(f => f.Opcode < WebSocketOpcode.Close).ToList();
                Assert.Equal(2, nonControlFrames.Count);
                Assert.False(nonControlFrames[0].EndOfMessage);
                Assert.True(nonControlFrames[1].EndOfMessage);
                Assert.Equal(WebSocketOpcode.Text, nonControlFrames[0].Opcode);
                Assert.Equal(WebSocketOpcode.Continuation, nonControlFrames[1].Opcode);
                Assert.Equal("Hello", Encoding.UTF8.GetString(nonControlFrames[0].Payload.ToArray()));
                Assert.Equal("World", Encoding.UTF8.GetString(nonControlFrames[1].Payload.ToArray()));
            }
        }
    }
}
