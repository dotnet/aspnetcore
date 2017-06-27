// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.SignalR.Tests.Common;
using Microsoft.AspNetCore.Sockets.Internal;
using Microsoft.AspNetCore.Sockets.Internal.Transports;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Tests
{
    public class WebSocketsTests
    {
        [Theory]
        [InlineData(WebSocketMessageType.Text)]
        [InlineData(WebSocketMessageType.Binary)]
        public async Task ReceivedFramesAreWrittenToChannel(WebSocketMessageType webSocketMessageType)
        {
            var transportToApplication = Channel.CreateUnbounded<byte[]>();
            var applicationToTransport = Channel.CreateUnbounded<byte[]>();

            using (var transportSide = ChannelConnection.Create<byte[]>(applicationToTransport, transportToApplication))
            using (var applicationSide = ChannelConnection.Create<byte[]>(transportToApplication, applicationToTransport))
            using (var feature = new TestWebSocketConnectionFeature())
            {
                var ws = new WebSocketsTransport(new WebSocketOptions(), transportSide, connectionId: string.Empty, loggerFactory: new LoggerFactory());

                // Give the server socket to the transport and run it
                var transport = ws.ProcessSocketAsync(await feature.AcceptAsync());

                // Run the client socket
                var client = feature.Client.ExecuteAndCaptureFramesAsync();

                // Send a frame, then close
                await feature.Client.SendAsync(
                    buffer: new ArraySegment<byte>(Encoding.UTF8.GetBytes("Hello")),
                    messageType: webSocketMessageType,
                    endOfMessage: true,
                    cancellationToken: CancellationToken.None);
                await feature.Client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);

                var buffer = await applicationSide.In.ReadAsync();
                Assert.Equal("Hello", Encoding.UTF8.GetString(buffer));

                Assert.True(applicationSide.Out.TryComplete());

                // The transport should finish now
                await transport;

                // The connection should close after this, which means the client will get a close frame.
                var clientSummary = await client;

                Assert.Equal(WebSocketCloseStatus.NormalClosure, clientSummary.CloseResult.CloseStatus);
            }
        }

        [Theory]
        [InlineData(WebSocketMessageType.Text)]
        [InlineData(WebSocketMessageType.Binary)]
        public async Task DataWrittenToOutputPipelineAreSentAsFrames(WebSocketMessageType webSocketMessageType)
        {
            var transportToApplication = Channel.CreateUnbounded<byte[]>();
            var applicationToTransport = Channel.CreateUnbounded<byte[]>();

            using (var transportSide = ChannelConnection.Create<byte[]>(applicationToTransport, transportToApplication))
            using (var applicationSide = ChannelConnection.Create<byte[]>(transportToApplication, applicationToTransport))
            using (var feature = new TestWebSocketConnectionFeature())
            {
                var ws = new WebSocketsTransport(new WebSocketOptions() { WebSocketMessageType = webSocketMessageType }, transportSide, connectionId: string.Empty, loggerFactory: new LoggerFactory());

                // Give the server socket to the transport and run it
                var transport = ws.ProcessSocketAsync(await feature.AcceptAsync());

                // Run the client socket
                var client = feature.Client.ExecuteAndCaptureFramesAsync();

                // Write to the output channel, and then complete it
                await applicationSide.Out.WriteAsync(Encoding.UTF8.GetBytes("Hello"));
                Assert.True(applicationSide.Out.TryComplete());

                // The client should finish now, as should the server
                var clientSummary = await client;
                await feature.Client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                await transport;

                Assert.Equal(1, clientSummary.Received.Count);
                Assert.True(clientSummary.Received[0].EndOfMessage);
                Assert.Equal(webSocketMessageType, clientSummary.Received[0].MessageType);
                Assert.Equal("Hello", Encoding.UTF8.GetString(clientSummary.Received[0].Buffer));
            }
        }

        [Fact]
        public async Task TransportFailsWhenClientDisconnectsAbnormally()
        {
            var transportToApplication = Channel.CreateUnbounded<byte[]>();
            var applicationToTransport = Channel.CreateUnbounded<byte[]>();

            using (var transportSide = ChannelConnection.Create<byte[]>(applicationToTransport, transportToApplication))
            using (var applicationSide = ChannelConnection.Create<byte[]>(transportToApplication, applicationToTransport))
            using (var feature = new TestWebSocketConnectionFeature())
            {
                var options = new WebSocketOptions()
                {
                    CloseTimeout = TimeSpan.FromMilliseconds(100)
                };

                var ws = new WebSocketsTransport(options, transportSide, connectionId: string.Empty, loggerFactory: new LoggerFactory());

                // Give the server socket to the transport and run it
                var transport = ws.ProcessSocketAsync(await feature.AcceptAsync());

                // Run the client socket
                var client = feature.Client.ExecuteAndCaptureFramesAsync();

                // Terminate the client to server channel with an exception
                feature.Client.Abort();

                // Wait for the transport
                await Assert.ThrowsAsync<OperationCanceledException>(() => transport).OrTimeout();

                var summary = await client.OrTimeout();
                Assert.Equal(WebSocketCloseStatus.InternalServerError, summary.CloseResult.CloseStatus);
            }
        }

        [Fact]
        public async Task ClientReceivesInternalServerErrorWhenTheApplicationFails()
        {
            var transportToApplication = Channel.CreateUnbounded<byte[]>();
            var applicationToTransport = Channel.CreateUnbounded<byte[]>();

            using (var transportSide = ChannelConnection.Create<byte[]>(applicationToTransport, transportToApplication))
            using (var applicationSide = ChannelConnection.Create<byte[]>(transportToApplication, applicationToTransport))
            using (var feature = new TestWebSocketConnectionFeature())
            {
                var ws = new WebSocketsTransport(new WebSocketOptions(), transportSide, connectionId: string.Empty, loggerFactory: new LoggerFactory());

                // Give the server socket to the transport and run it
                var transport = ws.ProcessSocketAsync(await feature.AcceptAsync());

                // Run the client socket
                var client = feature.Client.ExecuteAndCaptureFramesAsync();

                // Fail in the app
                Assert.True(applicationSide.Out.TryComplete(new InvalidOperationException("Catastrophic failure.")));
                var clientSummary = await client.OrTimeout();
                Assert.Equal(WebSocketCloseStatus.InternalServerError, clientSummary.CloseResult.CloseStatus);

                // Close from the client
                await feature.Client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);

                var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => transport.OrTimeout());
                Assert.Equal("Catastrophic failure.", ex.Message);
            }
        }

        [Fact]
        public async Task TransportClosesOnCloseTimeoutIfClientDoesNotSendCloseFrame()
        {
            var transportToApplication = Channel.CreateUnbounded<byte[]>();
            var applicationToTransport = Channel.CreateUnbounded<byte[]>();

            using (var transportSide = ChannelConnection.Create<byte[]>(applicationToTransport, transportToApplication))
            using (var applicationSide = ChannelConnection.Create<byte[]>(transportToApplication, applicationToTransport))
            using (var feature = new TestWebSocketConnectionFeature())
            {
                var options = new WebSocketOptions()
                {
                    CloseTimeout = TimeSpan.FromSeconds(1)
                };

                var ws = new WebSocketsTransport(options, transportSide, connectionId: string.Empty, loggerFactory: new LoggerFactory());

                var serverSocket = await feature.AcceptAsync();
                // Give the server socket to the transport and run it
                var transport = ws.ProcessSocketAsync(serverSocket);

                // End the app
                applicationSide.Dispose();

                await transport.OrTimeout(TimeSpan.FromSeconds(10));

                // Now we're closed
                Assert.Equal(WebSocketState.Aborted, serverSocket.State);

                serverSocket.Dispose();
            }
        }

        [Fact]
        public async Task TransportFailsOnTimeoutWithErrorWhenApplicationFailsAndClientDoesNotSendCloseFrame()
        {
            var transportToApplication = Channel.CreateUnbounded<byte[]>();
            var applicationToTransport = Channel.CreateUnbounded<byte[]>();

            using (var transportSide = ChannelConnection.Create<byte[]>(applicationToTransport, transportToApplication))
            using (var applicationSide = ChannelConnection.Create<byte[]>(transportToApplication, applicationToTransport))
            using (var feature = new TestWebSocketConnectionFeature())
            {
                var options = new WebSocketOptions()
                {
                    CloseTimeout = TimeSpan.FromSeconds(1)
                };

                var ws = new WebSocketsTransport(options, transportSide, connectionId: string.Empty, loggerFactory: new LoggerFactory());

                var serverSocket = await feature.AcceptAsync();
                // Give the server socket to the transport and run it
                var transport = ws.ProcessSocketAsync(serverSocket);

                // Run the client socket
                var client = feature.Client.ExecuteAndCaptureFramesAsync();

                // fail the client to server channel
                applicationToTransport.Out.TryComplete(new Exception());

                await Assert.ThrowsAsync<Exception>(() => transport).OrTimeout();

                Assert.Equal(WebSocketState.Aborted, serverSocket.State);
            }
        }

        [Fact]
        public async Task ServerGracefullyClosesWhenApplicationEndsThenClientSendsCloseFrame()
        {
            var transportToApplication = Channel.CreateUnbounded<byte[]>();
            var applicationToTransport = Channel.CreateUnbounded<byte[]>();

            using (var transportSide = ChannelConnection.Create<byte[]>(applicationToTransport, transportToApplication))
            using (var applicationSide = ChannelConnection.Create<byte[]>(transportToApplication, applicationToTransport))
            using (var feature = new TestWebSocketConnectionFeature())
            {
                var options = new WebSocketOptions()
                {
                    // We want to verify behavior without timeout affecting it
                    CloseTimeout = TimeSpan.FromSeconds(20)
                };
                var ws = new WebSocketsTransport(options, transportSide, connectionId: string.Empty, loggerFactory: new LoggerFactory());

                var serverSocket = await feature.AcceptAsync();
                // Give the server socket to the transport and run it
                var transport = ws.ProcessSocketAsync(serverSocket);

                // Run the client socket
                var client = feature.Client.ExecuteAndCaptureFramesAsync();

                // close the client to server channel
                applicationToTransport.Out.TryComplete();

                _ = await client.OrTimeout();

                await feature.Client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None).OrTimeout();

                await transport.OrTimeout();

                Assert.Equal(WebSocketCloseStatus.NormalClosure, serverSocket.CloseStatus);
            }
        }

        [Fact]
        public async Task ServerGracefullyClosesWhenClientSendsCloseFrameThenApplicationEnds()
        {
            var transportToApplication = Channel.CreateUnbounded<byte[]>();
            var applicationToTransport = Channel.CreateUnbounded<byte[]>();

            using (var transportSide = ChannelConnection.Create<byte[]>(applicationToTransport, transportToApplication))
            using (var applicationSide = ChannelConnection.Create<byte[]>(transportToApplication, applicationToTransport))
            using (var feature = new TestWebSocketConnectionFeature())
            {
                var options = new WebSocketOptions()
                {
                    // We want to verify behavior without timeout affecting it
                    CloseTimeout = TimeSpan.FromSeconds(20)
                };
                var ws = new WebSocketsTransport(options, transportSide, connectionId: string.Empty, loggerFactory: new LoggerFactory());

                var serverSocket = await feature.AcceptAsync();
                // Give the server socket to the transport and run it
                var transport = ws.ProcessSocketAsync(serverSocket);

                // Run the client socket
                var client = feature.Client.ExecuteAndCaptureFramesAsync();

                await feature.Client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None).OrTimeout();

                // close the client to server channel
                applicationToTransport.Out.TryComplete();

                _ = await client.OrTimeout();

                await transport.OrTimeout();

                Assert.Equal(WebSocketCloseStatus.NormalClosure, serverSocket.CloseStatus);
            }
        }
    }
}
