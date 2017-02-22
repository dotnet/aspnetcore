// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.SignalR.Tests.Common;
using Microsoft.AspNetCore.Sockets.Internal;
using Microsoft.AspNetCore.Sockets.Transports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.WebSockets.Internal;
using Microsoft.Extensions.WebSockets.Internal.Tests;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Tests
{
    public class WebSocketsTests
    {
        [Theory]
        [InlineData(MessageType.Text, WebSocketOpcode.Text)]
        [InlineData(MessageType.Binary, WebSocketOpcode.Binary)]
        public async Task ReceivedFramesAreWrittenToChannel(MessageType format, WebSocketOpcode opcode)
        {
            var transportToApplication = Channel.CreateUnbounded<Message>();
            var applicationToTransport = Channel.CreateUnbounded<Message>();

            var transportSide = new ChannelConnection<Message>(applicationToTransport, transportToApplication);
            var applicationSide = new ChannelConnection<Message>(transportToApplication, applicationToTransport);

            using (var factory = new PipeFactory())
            using (var pair = WebSocketPair.Create(factory))
            {
                var ws = new WebSocketsTransport(transportSide, new LoggerFactory());

                // Give the server socket to the transport and run it
                var transport = ws.ProcessSocketAsync(pair.ServerSocket);

                // Run the client socket
                var client = pair.ClientSocket.ExecuteAndCaptureFramesAsync();

                // Send a frame, then close
                await pair.ClientSocket.SendAsync(new WebSocketFrame(
                    endOfMessage: true,
                    opcode: opcode,
                    payload: ReadableBuffer.Create(Encoding.UTF8.GetBytes("Hello"))));
                await pair.ClientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure);

                var message = await applicationSide.Input.In.ReadAsync();
                Assert.True(message.EndOfMessage);
                Assert.Equal(format, message.Type);
                Assert.Equal("Hello", Encoding.UTF8.GetString(message.Payload));

                Assert.True(applicationSide.Output.Out.TryComplete());

                // The transport should finish now
                await transport;

                // The connection should close after this, which means the client will get a close frame.
                var clientSummary = await client;

                Assert.Equal(WebSocketCloseStatus.NormalClosure, clientSummary.CloseResult.Status);
            }
        }

        [Theory]
        [InlineData(MessageType.Text, WebSocketOpcode.Text)]
        [InlineData(MessageType.Binary, WebSocketOpcode.Binary)]
        public async Task MultiFrameMessagesArePropagatedToTheChannel(MessageType format, WebSocketOpcode opcode)
        {
            var transportToApplication = Channel.CreateUnbounded<Message>();
            var applicationToTransport = Channel.CreateUnbounded<Message>();

            var transportSide = new ChannelConnection<Message>(applicationToTransport, transportToApplication);
            var applicationSide = new ChannelConnection<Message>(transportToApplication, applicationToTransport);

            using (var factory = new PipeFactory())
            using (var pair = WebSocketPair.Create(factory))
            {
                var ws = new WebSocketsTransport(transportSide, new LoggerFactory());

                // Give the server socket to the transport and run it
                var transport = ws.ProcessSocketAsync(pair.ServerSocket);

                // Run the client socket
                var client = pair.ClientSocket.ExecuteAndCaptureFramesAsync();

                // Send a frame, then close
                await pair.ClientSocket.SendAsync(new WebSocketFrame(
                    endOfMessage: false,
                    opcode: opcode,
                    payload: ReadableBuffer.Create(Encoding.UTF8.GetBytes("Hello"))));
                await pair.ClientSocket.SendAsync(new WebSocketFrame(
                    endOfMessage: true,
                    opcode: WebSocketOpcode.Continuation,
                    payload: ReadableBuffer.Create(Encoding.UTF8.GetBytes("World"))));
                await pair.ClientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure);

                var message1 = await applicationSide.Input.In.ReadAsync();
                Assert.False(message1.EndOfMessage);
                Assert.Equal(format, message1.Type);
                Assert.Equal("Hello", Encoding.UTF8.GetString(message1.Payload));

                var message2 = await applicationSide.Input.In.ReadAsync();
                Assert.True(message2.EndOfMessage);
                Assert.Equal(format, message2.Type);
                Assert.Equal("World", Encoding.UTF8.GetString(message2.Payload));

                Assert.True(applicationSide.Output.Out.TryComplete());

                // The transport should finish now
                await transport;

                // The connection should close after this, which means the client will get a close frame.
                var clientSummary = await client;

                Assert.Equal(WebSocketCloseStatus.NormalClosure, clientSummary.CloseResult.Status);
            }
        }

        [Theory]
        [InlineData(MessageType.Text, WebSocketOpcode.Text)]
        [InlineData(MessageType.Binary, WebSocketOpcode.Binary)]
        public async Task IncompleteMessagesAreWrittenAsMultiFrameWebSocketMessages(MessageType format, WebSocketOpcode opcode)
        {
            var transportToApplication = Channel.CreateUnbounded<Message>();
            var applicationToTransport = Channel.CreateUnbounded<Message>();

            var transportSide = new ChannelConnection<Message>(applicationToTransport, transportToApplication);
            var applicationSide = new ChannelConnection<Message>(transportToApplication, applicationToTransport);

            using (var factory = new PipeFactory())
            using (var pair = WebSocketPair.Create(factory))
            {
                var ws = new WebSocketsTransport(transportSide, new LoggerFactory());

                // Give the server socket to the transport and run it
                var transport = ws.ProcessSocketAsync(pair.ServerSocket);

                // Run the client socket
                var client = pair.ClientSocket.ExecuteAndCaptureFramesAsync();

                // Write multi-frame message to the output channel, and then complete it
                await applicationSide.Output.Out.WriteAsync(new Message(
                    Encoding.UTF8.GetBytes("Hello"),
                    format,
                    endOfMessage: false));
                await applicationSide.Output.Out.WriteAsync(new Message(
                    Encoding.UTF8.GetBytes("World"),
                    format,
                    endOfMessage: true));
                Assert.True(applicationSide.Output.Out.TryComplete());

                // The client should finish now, as should the server
                var clientSummary = await client;
                await pair.ClientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure);
                await transport;

                Assert.Equal(2, clientSummary.Received.Count);
                Assert.False(clientSummary.Received[0].EndOfMessage);
                Assert.Equal(opcode, clientSummary.Received[0].Opcode);
                Assert.Equal("Hello", Encoding.UTF8.GetString(clientSummary.Received[0].Payload.ToArray()));
                Assert.True(clientSummary.Received[1].EndOfMessage);
                Assert.Equal(WebSocketOpcode.Continuation, clientSummary.Received[1].Opcode);
                Assert.Equal("World", Encoding.UTF8.GetString(clientSummary.Received[1].Payload.ToArray()));
            }
        }

        [Theory]
        [InlineData(MessageType.Text, WebSocketOpcode.Text)]
        [InlineData(MessageType.Binary, WebSocketOpcode.Binary)]
        public async Task DataWrittenToOutputPipelineAreSentAsFrames(MessageType format, WebSocketOpcode opcode)
        {
            var transportToApplication = Channel.CreateUnbounded<Message>();
            var applicationToTransport = Channel.CreateUnbounded<Message>();

            var transportSide = new ChannelConnection<Message>(applicationToTransport, transportToApplication);
            var applicationSide = new ChannelConnection<Message>(transportToApplication, applicationToTransport);

            using (var factory = new PipeFactory())
            using (var pair = WebSocketPair.Create(factory))
            {
                var ws = new WebSocketsTransport(transportSide, new LoggerFactory());

                // Give the server socket to the transport and run it
                var transport = ws.ProcessSocketAsync(pair.ServerSocket);

                // Run the client socket
                var client = pair.ClientSocket.ExecuteAndCaptureFramesAsync();

                // Write to the output channel, and then complete it
                await applicationSide.Output.Out.WriteAsync(new Message(
                    Encoding.UTF8.GetBytes("Hello"),
                    format,
                    endOfMessage: true));
                Assert.True(applicationSide.Output.Out.TryComplete());

                // The client should finish now, as should the server
                var clientSummary = await client;
                await pair.ClientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure);
                await transport;

                Assert.Equal(1, clientSummary.Received.Count);
                Assert.True(clientSummary.Received[0].EndOfMessage);
                Assert.Equal(opcode, clientSummary.Received[0].Opcode);
                Assert.Equal("Hello", Encoding.UTF8.GetString(clientSummary.Received[0].Payload.ToArray()));
            }
        }

        [Theory]
        [InlineData(MessageType.Text, WebSocketOpcode.Text)]
        [InlineData(MessageType.Binary, WebSocketOpcode.Binary)]
        public async Task FrameReceivedAfterServerCloseSent(MessageType format, WebSocketOpcode opcode)
        {
            var transportToApplication = Channel.CreateUnbounded<Message>();
            var applicationToTransport = Channel.CreateUnbounded<Message>();

            var transportSide = new ChannelConnection<Message>(applicationToTransport, transportToApplication);
            var applicationSide = new ChannelConnection<Message>(transportToApplication, applicationToTransport);

            using (var factory = new PipeFactory())
            using (var pair = WebSocketPair.Create(factory))
            {
                var ws = new WebSocketsTransport(transportSide, new LoggerFactory());

                // Give the server socket to the transport and run it
                var transport = ws.ProcessSocketAsync(pair.ServerSocket);

                // Run the client socket
                var client = pair.ClientSocket.ExecuteAndCaptureFramesAsync();

                // Close the output and wait for the close frame
                Assert.True(applicationSide.Output.Out.TryComplete());
                await client;

                // Send another frame. Then close
                await pair.ClientSocket.SendAsync(new WebSocketFrame(
                    endOfMessage: true,
                    opcode: opcode,
                    payload: ReadableBuffer.Create(Encoding.UTF8.GetBytes("Hello"))));
                await pair.ClientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure);

                // Read that frame from the input
                var message = await applicationSide.Input.In.ReadAsync();
                Assert.True(message.EndOfMessage);
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

            using (var factory = new PipeFactory())
            using (var pair = WebSocketPair.Create(factory))
            {
                var ws = new WebSocketsTransport(transportSide, new LoggerFactory());

                // Give the server socket to the transport and run it
                var transport = ws.ProcessSocketAsync(pair.ServerSocket);

                // Run the client socket
                var client = pair.ClientSocket.ExecuteAndCaptureFramesAsync();

                // Terminate the client to server channel with an exception
                pair.TerminateFromClient(new InvalidOperationException());

                // Wait for the transport
                await Assert.ThrowsAsync<InvalidOperationException>(() => transport);
            }
        }

        [Fact]
        public async Task ClientReceivesInternalServerErrorWhenTheApplicationFails()
        {
            var transportToApplication = Channel.CreateUnbounded<Message>();
            var applicationToTransport = Channel.CreateUnbounded<Message>();

            var transportSide = new ChannelConnection<Message>(applicationToTransport, transportToApplication);
            var applicationSide = new ChannelConnection<Message>(transportToApplication, applicationToTransport);

            using (var factory = new PipeFactory())
            using (var pair = WebSocketPair.Create(factory))
            {
                var ws = new WebSocketsTransport(transportSide, new LoggerFactory());

                // Give the server socket to the transport and run it
                var transport = ws.ProcessSocketAsync(pair.ServerSocket);

                // Run the client socket
                var client = pair.ClientSocket.ExecuteAndCaptureFramesAsync();

                // Fail in the app
                Assert.True(applicationSide.Output.Out.TryComplete(new InvalidOperationException()));
                var clientSummary = await client;
                Assert.Equal(WebSocketCloseStatus.InternalServerError, clientSummary.CloseResult.Status);

                // Close from the client
                await pair.ClientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure);

                await transport.OrTimeout();
            }
        }
    }
}
