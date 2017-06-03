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
using Microsoft.AspNetCore.Sockets.Transports;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Tests
{
    public class WebSocketsTests
    {
        [Theory]
        [InlineData(MessageType.Text, WebSocketMessageType.Text)]
        [InlineData(MessageType.Binary, WebSocketMessageType.Binary)]
        public async Task ReceivedFramesAreWrittenToChannel(MessageType format, WebSocketMessageType webSocketMessageType)
        {
            var transportToApplication = Channel.CreateUnbounded<Message>();
            var applicationToTransport = Channel.CreateUnbounded<Message>();

            var transportSide = new ChannelConnection<Message>(applicationToTransport, transportToApplication);
            var applicationSide = new ChannelConnection<Message>(transportToApplication, applicationToTransport);

            using (var feature = new TestWebSocketConnectionFeature())
            {
                var ws = new WebSocketsTransport(new WebSocketOptions(), transportSide, new LoggerFactory());

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

                var message = await applicationSide.Input.In.ReadAsync();
                Assert.Equal(format, message.Type);
                Assert.Equal("Hello", Encoding.UTF8.GetString(message.Payload));

                Assert.True(applicationSide.Output.Out.TryComplete());

                // The transport should finish now
                await transport;

                // The connection should close after this, which means the client will get a close frame.
                var clientSummary = await client;

                Assert.Equal(WebSocketCloseStatus.NormalClosure, clientSummary.CloseResult.CloseStatus);
            }
        }

        [Theory]
        [InlineData(MessageType.Text, WebSocketMessageType.Text)]
        [InlineData(MessageType.Binary, WebSocketMessageType.Binary)]
        public async Task DataWrittenToOutputPipelineAreSentAsFrames(MessageType format, WebSocketMessageType webSocketMessageType)
        {
            var transportToApplication = Channel.CreateUnbounded<Message>();
            var applicationToTransport = Channel.CreateUnbounded<Message>();

            var transportSide = new ChannelConnection<Message>(applicationToTransport, transportToApplication);
            var applicationSide = new ChannelConnection<Message>(transportToApplication, applicationToTransport);

            using (var feature = new TestWebSocketConnectionFeature())
            {
                var ws = new WebSocketsTransport(new WebSocketOptions(), transportSide, new LoggerFactory());

                // Give the server socket to the transport and run it
                var transport = ws.ProcessSocketAsync(await feature.AcceptAsync());

                // Run the client socket
                var client = feature.Client.ExecuteAndCaptureFramesAsync();

                // Write to the output channel, and then complete it
                await applicationSide.Output.Out.WriteAsync(new Message(
                    Encoding.UTF8.GetBytes("Hello"),
                    format));
                Assert.True(applicationSide.Output.Out.TryComplete());

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

        [Theory]
        [InlineData(MessageType.Text, WebSocketMessageType.Text)]
        [InlineData(MessageType.Binary, WebSocketMessageType.Binary)]
        public async Task FrameReceivedAfterServerCloseSent(MessageType format, WebSocketMessageType webSocketMessageType)
        {
            var transportToApplication = Channel.CreateUnbounded<Message>();
            var applicationToTransport = Channel.CreateUnbounded<Message>();

            var transportSide = new ChannelConnection<Message>(applicationToTransport, transportToApplication);
            var applicationSide = new ChannelConnection<Message>(transportToApplication, applicationToTransport);

            using (var feature = new TestWebSocketConnectionFeature())
            {
                var ws = new WebSocketsTransport(new WebSocketOptions(), transportSide, new LoggerFactory());

                // Give the server socket to the transport and run it
                var transport = ws.ProcessSocketAsync(await feature.AcceptAsync());

                // Run the client socket
                var client = feature.Client.ExecuteAndCaptureFramesAsync();

                // Close the output and wait for the close frame
                Assert.True(applicationSide.Output.Out.TryComplete());
                await client;

                // Send another frame. Then close
                await feature.Client.SendAsync(
                    buffer: new ArraySegment<byte>(Encoding.UTF8.GetBytes("Hello")),
                    endOfMessage: true,
                    messageType: webSocketMessageType,
                    cancellationToken: CancellationToken.None);
                await feature.Client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);

                // Read that frame from the input
                var message = await applicationSide.Input.In.ReadAsync();
                Assert.Equal(format, message.Type);
                Assert.Equal("Hello", Encoding.UTF8.GetString(message.Payload));

                await transport;
            }
        }

        [Fact]
        public async Task TransportFailsWhenClientDisconnectsAbnormally()
        {
            var transportToApplication = Channel.CreateUnbounded<Message>();
            var applicationToTransport = Channel.CreateUnbounded<Message>();

            var transportSide = new ChannelConnection<Message>(applicationToTransport, transportToApplication);
            var applicationSide = new ChannelConnection<Message>(transportToApplication, applicationToTransport);

            using (var feature = new TestWebSocketConnectionFeature())
            {
                var ws = new WebSocketsTransport(new WebSocketOptions(), transportSide, new LoggerFactory());

                // Give the server socket to the transport and run it
                var transport = ws.ProcessSocketAsync(await feature.AcceptAsync());

                // Run the client socket
                var client = feature.Client.ExecuteAndCaptureFramesAsync();

                // Terminate the client to server channel with an exception
                feature.Client.Abort();

                // Wait for the transport
                await Assert.ThrowsAsync<OperationCanceledException>(() => transport);
            }
        }

        [Fact]
        public async Task ClientReceivesInternalServerErrorWhenTheApplicationFails()
        {
            var transportToApplication = Channel.CreateUnbounded<Message>();
            var applicationToTransport = Channel.CreateUnbounded<Message>();

            var transportSide = new ChannelConnection<Message>(applicationToTransport, transportToApplication);
            var applicationSide = new ChannelConnection<Message>(transportToApplication, applicationToTransport);

            using (var feature = new TestWebSocketConnectionFeature())
            {
                var ws = new WebSocketsTransport(new WebSocketOptions(), transportSide, new LoggerFactory());

                // Give the server socket to the transport and run it
                var transport = ws.ProcessSocketAsync(await feature.AcceptAsync());

                // Run the client socket
                var client = feature.Client.ExecuteAndCaptureFramesAsync();

                // Fail in the app
                Assert.True(applicationSide.Output.Out.TryComplete(new InvalidOperationException()));
                var clientSummary = await client;
                Assert.Equal(WebSocketCloseStatus.InternalServerError, clientSummary.CloseResult.CloseStatus);

                // Close from the client
                await feature.Client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);

                await transport.OrTimeout();
            }
        }

        [Fact]
        public async Task TransportClosesOnCloseTimeoutIfClientDoesNotSendCloseFrame()
        {
            var transportToApplication = Channel.CreateUnbounded<Message>();
            var applicationToTransport = Channel.CreateUnbounded<Message>();

            var transportSide = new ChannelConnection<Message>(applicationToTransport, transportToApplication);
            var applicationSide = new ChannelConnection<Message>(transportToApplication, applicationToTransport);

            using (var feature = new TestWebSocketConnectionFeature())
            {
                var options = new WebSocketOptions()
                {
                    CloseTimeout = TimeSpan.FromSeconds(1)
                };

                var ws = new WebSocketsTransport(options, transportSide, new LoggerFactory());

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
    }
}
