using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Channels;
using Microsoft.Extensions.WebSockets.Test;
using Xunit;

namespace Microsoft.Extensions.WebSockets.Tests
{
    public partial class WebSocketConnectionTests
    {
        [Fact]
        public async Task SendReceiveFrames()
        {
            using (var pair = WebSocketPair.Create())
            {
                var cts = new CancellationTokenSource();
                if (!Debugger.IsAttached)
                {
                    cts.CancelAfter(TimeSpan.FromSeconds(5));
                }
                using (cts.Token.Register(() => pair.Dispose()))
                {
                    var client = pair.ClientSocket.ExecuteAsync(_ =>
                    {
                        Assert.False(true, "did not expect the client to receive any frames!");
                        return Task.CompletedTask;
                    });

                    // Send Frames
                    await pair.ClientSocket.SendAsync(CreateTextFrame("Hello"));
                    await pair.ClientSocket.SendAsync(CreateTextFrame("World"));
                    await pair.ClientSocket.SendAsync(CreateBinaryFrame(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }));
                    await pair.ClientSocket.CloseAsync(new WebSocketCloseResult(WebSocketCloseStatus.NormalClosure));

                    var summary = await pair.ServerSocket.ExecuteAndCaptureFramesAsync();
                    Assert.Equal(3, summary.Received.Count);
                    Assert.Equal("Hello", Encoding.UTF8.GetString(summary.Received[0].Payload.ToArray()));
                    Assert.Equal("World", Encoding.UTF8.GetString(summary.Received[1].Payload.ToArray()));
                    Assert.Equal(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, summary.Received[2].Payload.ToArray());

                    await pair.ServerSocket.CloseAsync(new WebSocketCloseResult(WebSocketCloseStatus.NormalClosure));
                    await client;
                }
            }
        }

        [Fact]
        public async Task ExecuteReturnsWhenCloseFrameReceived()
        {
            using(var pair = WebSocketPair.Create())
            {
                var client = pair.ClientSocket.ExecuteAndCaptureFramesAsync();
                await pair.ClientSocket.CloseAsync(new WebSocketCloseResult(WebSocketCloseStatus.InvalidMessageType, "Abc"));
                var serverSummary = await pair.ServerSocket.ExecuteAndCaptureFramesAsync();
                await pair.ServerSocket.CloseAsync(new WebSocketCloseResult(WebSocketCloseStatus.NormalClosure, "Ok"));
                var clientSummary = await client;

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
            using(var pair = WebSocketPair.Create())
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
                await pair.ClientSocket.SendAsync(CreateTextFrame("Hello"));
                await pair.ServerSocket.SendAsync(CreateTextFrame("Hello"));

                await Task.WhenAll(serverReceiving.Task, clientReceiving.Task);

                // Check state
                Assert.Equal(WebSocketConnectionState.Connected, pair.ServerSocket.State);
                Assert.Equal(WebSocketConnectionState.Connected, pair.ClientSocket.State);

                // Close the server socket
                await pair.ServerSocket.CloseAsync(new WebSocketCloseResult(WebSocketCloseStatus.NormalClosure));
                await client;

                // Check state
                Assert.Equal(WebSocketConnectionState.CloseSent, pair.ServerSocket.State);
                Assert.Equal(WebSocketConnectionState.CloseReceived, pair.ClientSocket.State);

                // Close the client socket
                await pair.ClientSocket.CloseAsync(new WebSocketCloseResult(WebSocketCloseStatus.NormalClosure));
                await server;

                // Check state
                Assert.Equal(WebSocketConnectionState.Closed, pair.ServerSocket.State);
                Assert.Equal(WebSocketConnectionState.Closed, pair.ClientSocket.State);

                // Verify we can't restart the connection or send a message
                await Assert.ThrowsAsync<ObjectDisposedException>(async () => await pair.ServerSocket.ExecuteAsync(f => { }));
                await Assert.ThrowsAsync<ObjectDisposedException>(async () => await pair.ClientSocket.SendAsync(CreateTextFrame("Nope")));
                await Assert.ThrowsAsync<ObjectDisposedException>(async () => await pair.ClientSocket.CloseAsync(new WebSocketCloseResult(WebSocketCloseStatus.NormalClosure)));
            }
        }
    }
}
