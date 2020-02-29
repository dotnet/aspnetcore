// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Internal;
using Microsoft.AspNetCore.Http.Connections.Internal.Transports;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Http.Connections.Tests
{
    public class WebSocketsTests : VerifiableLoggedTest
    {
        // Using nameof with WebSocketMessageType because it is a GACed type and xunit can't serialize it
        [Theory]
        [InlineData(nameof(WebSocketMessageType.Text))]
        [InlineData(nameof(WebSocketMessageType.Binary))]
        public async Task ReceivedFramesAreWrittenToChannel(string webSocketMessageType)
        {
            using (StartVerifiableLog())
            {
                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                var connection = new HttpConnectionContext("foo", pair.Transport, pair.Application, LoggerFactory.CreateLogger("HttpConnectionContext1"));

                using (var feature = new TestWebSocketConnectionFeature())
                {
                    var connectionContext = new HttpConnectionContext(string.Empty, null, null, LoggerFactory.CreateLogger("HttpConnectionContext2"));
                    var ws = new WebSocketsServerTransport(new WebSocketOptions(), connection.Application, connectionContext, LoggerFactory);

                    // Give the server socket to the transport and run it
                    var transport = ws.ProcessSocketAsync(await feature.AcceptAsync());

                    // Run the client socket
                    var client = feature.Client.ExecuteAndCaptureFramesAsync();

                    // Send a frame, then close
                    await feature.Client.SendAsync(
                        buffer: new ArraySegment<byte>(Encoding.UTF8.GetBytes("Hello")),
                        messageType: (WebSocketMessageType)Enum.Parse(typeof(WebSocketMessageType), webSocketMessageType),
                        endOfMessage: true,
                        cancellationToken: CancellationToken.None);
                    await feature.Client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);

                    var result = await connection.Transport.Input.ReadAsync();
                    var buffer = result.Buffer;
                    Assert.Equal("Hello", Encoding.UTF8.GetString(buffer.ToArray()));
                    connection.Transport.Input.AdvanceTo(buffer.End);

                    connection.Transport.Output.Complete();

                    // The transport should finish now
                    await transport;

                    // The connection should close after this, which means the client will get a close frame.
                    var clientSummary = await client;

                    Assert.Equal(WebSocketCloseStatus.NormalClosure, clientSummary.CloseResult.CloseStatus);
                }
            }
        }

        // Using nameof with WebSocketMessageType because it is a GACed type and xunit can't serialize it
        [Theory]
        [InlineData(TransferFormat.Text, nameof(WebSocketMessageType.Text))]
        [InlineData(TransferFormat.Binary, nameof(WebSocketMessageType.Binary))]
        public async Task WebSocketTransportSetsMessageTypeBasedOnTransferFormatFeature(TransferFormat transferFormat, string expectedMessageType)
        {
            using (StartVerifiableLog())
            {
                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                var connection = new HttpConnectionContext("foo", pair.Transport, pair.Application, LoggerFactory.CreateLogger("HttpConnectionContext1"));

                using (var feature = new TestWebSocketConnectionFeature())
                {
                    var connectionContext = new HttpConnectionContext(string.Empty, null, null, LoggerFactory.CreateLogger("HttpConnectionContext2"));
                    connectionContext.ActiveFormat = transferFormat;
                    var ws = new WebSocketsServerTransport(new WebSocketOptions(), connection.Application, connectionContext, LoggerFactory);

                    // Give the server socket to the transport and run it
                    var transport = ws.ProcessSocketAsync(await feature.AcceptAsync());

                    // Run the client socket
                    var client = feature.Client.ExecuteAndCaptureFramesAsync();

                    // Write to the output channel, and then complete it
                    await connection.Transport.Output.WriteAsync(Encoding.UTF8.GetBytes("Hello"));
                    connection.Transport.Output.Complete();

                    // The client should finish now, as should the server
                    var clientSummary = await client;
                    await feature.Client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    await transport;

                    Assert.Equal(1, clientSummary.Received.Count);
                    Assert.True(clientSummary.Received[0].EndOfMessage);
                    Assert.Equal((WebSocketMessageType)Enum.Parse(typeof(WebSocketMessageType), expectedMessageType), clientSummary.Received[0].MessageType);
                    Assert.Equal("Hello", Encoding.UTF8.GetString(clientSummary.Received[0].Buffer));
                }
            }
        }

        [Fact]
        public async Task TransportCommunicatesErrorToApplicationWhenClientDisconnectsAbnormally()
        {
            using (StartVerifiableLog())
            {
                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                var connection = new HttpConnectionContext("foo", pair.Transport, pair.Application, LoggerFactory.CreateLogger("HttpConnectionContext1"));

                using (var feature = new TestWebSocketConnectionFeature())
                {
                    async Task CompleteApplicationAfterTransportCompletes()
                    {
                        try
                        {
                            // Wait until the transport completes so that we can end the application
                            var result = await connection.Transport.Input.ReadAsync();
                            connection.Transport.Input.AdvanceTo(result.Buffer.End);
                        }
                        catch (Exception ex)
                        {
                            Assert.IsType<WebSocketError>(ex);
                        }
                        finally
                        {
                            // Complete the application so that the connection unwinds without aborting
                            connection.Transport.Output.Complete();
                        }
                    }

                    var connectionContext = new HttpConnectionContext(string.Empty, null, null, LoggerFactory.CreateLogger("HttpConnectionContext2"));
                    var ws = new WebSocketsServerTransport(new WebSocketOptions(), connection.Application, connectionContext, LoggerFactory);

                    // Give the server socket to the transport and run it
                    var transport = ws.ProcessSocketAsync(await feature.AcceptAsync());

                    // Run the client socket
                    var client = feature.Client.ExecuteAndCaptureFramesAsync();

                    // When the close frame is received, we complete the application so the send
                    // loop unwinds
                    _ = CompleteApplicationAfterTransportCompletes();

                    // Terminate the client to server channel with an exception
                    feature.Client.SendAbort();

                    // Wait for the transport
                    await transport.OrTimeout();

                    await client.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task ClientReceivesInternalServerErrorWhenTheApplicationFails()
        {
            using (StartVerifiableLog())
            {
                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                var connection = new HttpConnectionContext("foo", pair.Transport, pair.Application);

                using (var feature = new TestWebSocketConnectionFeature())
                {
                    var connectionContext = new HttpConnectionContext(string.Empty, null, null);
                    var ws = new WebSocketsServerTransport(new WebSocketOptions(), connection.Application, connectionContext, LoggerFactory);

                    // Give the server socket to the transport and run it
                    var transport = ws.ProcessSocketAsync(await feature.AcceptAsync());

                    // Run the client socket
                    var client = feature.Client.ExecuteAndCaptureFramesAsync();

                    // Fail in the app
                    connection.Transport.Output.Complete(new InvalidOperationException("Catastrophic failure."));
                    var clientSummary = await client.OrTimeout();
                    Assert.Equal(WebSocketCloseStatus.InternalServerError, clientSummary.CloseResult.CloseStatus);

                    // Close from the client
                    await feature.Client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);

                    await transport.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task TransportClosesOnCloseTimeoutIfClientDoesNotSendCloseFrame()
        {
            using (StartVerifiableLog())
            {
                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                var connection = new HttpConnectionContext("foo", pair.Transport, pair.Application);

                using (var feature = new TestWebSocketConnectionFeature())
                {
                    var options = new WebSocketOptions()
                    {
                        CloseTimeout = TimeSpan.FromSeconds(1)
                    };

                    var connectionContext = new HttpConnectionContext(string.Empty, null, null);
                    var ws = new WebSocketsServerTransport(options, connection.Application, connectionContext, LoggerFactory);

                    var serverSocket = await feature.AcceptAsync();
                    // Give the server socket to the transport and run it
                    var transport = ws.ProcessSocketAsync(serverSocket);

                    // End the app
                    connection.Transport.Output.Complete();

                    await transport.OrTimeout(TimeSpan.FromSeconds(10));

                    // Now we're closed
                    Assert.Equal(WebSocketState.Aborted, serverSocket.State);

                    serverSocket.Dispose();
                }
            }
        }

        [Fact]
        public async Task TransportFailsOnTimeoutWithErrorWhenApplicationFailsAndClientDoesNotSendCloseFrame()
        {
            using (StartVerifiableLog())
            {
                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                var connection = new HttpConnectionContext("foo", pair.Transport, pair.Application);

                using (var feature = new TestWebSocketConnectionFeature())
                {
                    var options = new WebSocketOptions
                    {
                        CloseTimeout = TimeSpan.FromSeconds(1)
                    };

                    var connectionContext = new HttpConnectionContext(string.Empty, null, null);
                    var ws = new WebSocketsServerTransport(options, connection.Application, connectionContext, LoggerFactory);

                    var serverSocket = await feature.AcceptAsync();
                    // Give the server socket to the transport and run it
                    var transport = ws.ProcessSocketAsync(serverSocket);

                    // Run the client socket
                    var client = feature.Client.ExecuteAndCaptureFramesAsync();

                    // fail the client to server channel
                    connection.Transport.Output.Complete(new Exception());

                    await transport.OrTimeout();

                    Assert.Equal(WebSocketState.Aborted, serverSocket.State);
                }
            }
        }

        [Fact]
        public async Task ServerGracefullyClosesWhenApplicationEndsThenClientSendsCloseFrame()
        {
            using (StartVerifiableLog())
            {
                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                var connection = new HttpConnectionContext("foo", pair.Transport, pair.Application);

                using (var feature = new TestWebSocketConnectionFeature())
                {
                    var options = new WebSocketOptions
                    {
                        // We want to verify behavior without timeout affecting it
                        CloseTimeout = TimeSpan.FromSeconds(20)
                    };

                    var connectionContext = new HttpConnectionContext(string.Empty, null, null);
                    var ws = new WebSocketsServerTransport(options, connection.Application, connectionContext, LoggerFactory);

                    var serverSocket = await feature.AcceptAsync();
                    // Give the server socket to the transport and run it
                    var transport = ws.ProcessSocketAsync(serverSocket);

                    // Run the client socket
                    var client = feature.Client.ExecuteAndCaptureFramesAsync();

                    // close the client to server channel
                    connection.Transport.Output.Complete();

                    _ = await client.OrTimeout();

                    await feature.Client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None).OrTimeout();

                    await transport.OrTimeout();

                    Assert.Equal(WebSocketCloseStatus.NormalClosure, serverSocket.CloseStatus);
                }
            }
        }

        [Fact]
        public async Task ServerGracefullyClosesWhenClientSendsCloseFrameThenApplicationEnds()
        {
            using (StartVerifiableLog())
            {
                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                var connection = new HttpConnectionContext("foo", pair.Transport, pair.Application);

                using (var feature = new TestWebSocketConnectionFeature())
                {
                    var options = new WebSocketOptions
                    {
                        // We want to verify behavior without timeout affecting it
                        CloseTimeout = TimeSpan.FromSeconds(20)
                    };

                    var connectionContext = new HttpConnectionContext(string.Empty, null, null);
                    var ws = new WebSocketsServerTransport(options, connection.Application, connectionContext, LoggerFactory);

                    var serverSocket = await feature.AcceptAsync();
                    // Give the server socket to the transport and run it
                    var transport = ws.ProcessSocketAsync(serverSocket);

                    // Run the client socket
                    var client = feature.Client.ExecuteAndCaptureFramesAsync();

                    await feature.Client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None).OrTimeout();

                    // close the client to server channel
                    connection.Transport.Output.Complete();

                    _ = await client.OrTimeout();

                    await transport.OrTimeout();

                    Assert.Equal(WebSocketCloseStatus.NormalClosure, serverSocket.CloseStatus);
                }
            }
        }

        [Fact]
        public async Task SubProtocolSelectorIsUsedToSelectSubProtocol()
        {
            const string ExpectedSubProtocol = "expected";
            var providedSubProtocols = new[] {"provided1", "provided2"};

            using (StartVerifiableLog())
            {
                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                var connection = new HttpConnectionContext("foo", pair.Transport, pair.Application);

                using (var feature = new TestWebSocketConnectionFeature())
                {
                    var options = new WebSocketOptions
                    {
                        // We want to verify behavior without timeout affecting it
                        CloseTimeout = TimeSpan.FromSeconds(20),
                        SubProtocolSelector = protocols => {
                            Assert.Equal(providedSubProtocols, protocols.ToArray());
                            return ExpectedSubProtocol;
                        },
                    };

                    var connectionContext = new HttpConnectionContext(string.Empty, null, null);
                    var ws = new WebSocketsServerTransport(options, connection.Application, connectionContext, LoggerFactory);

                    // Create an HttpContext
                    var context = new DefaultHttpContext();
                    context.Request.Headers.Add(HeaderNames.WebSocketSubProtocols, providedSubProtocols.ToArray());
                    context.Features.Set<IHttpWebSocketFeature>(feature);
                    var transport = ws.ProcessRequestAsync(context, CancellationToken.None);

                    await feature.Accepted.OrThrowIfOtherFails(transport);

                    // Assert the feature got the right subprotocol
                    Assert.Equal(ExpectedSubProtocol, feature.SubProtocol);

                    // Run the client socket
                    var client = feature.Client.ExecuteAndCaptureFramesAsync();

                    await feature.Client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None).OrTimeout();

                    // close the client to server channel
                    connection.Transport.Output.Complete();

                    _ = await client.OrTimeout();

                    await transport.OrTimeout();
                }
            }
        }

        [Fact]
        public async Task MultiSegmentSendWillNotSendEmptyEndOfMessageFrame()
        {
            using (var feature = new TestWebSocketConnectionFeature())
            {
                var serverSocket = await feature.AcceptAsync();
                var sequence = ReadOnlySequenceFactory.CreateSegments(new byte[] { 1 }, new byte[] { 15 });
                Assert.False(sequence.IsSingleSegment);

                await serverSocket.SendAsync(sequence, WebSocketMessageType.Text);

                // Run the client socket
                var client = feature.Client.ExecuteAndCaptureFramesAsync();

                await serverSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", default);

                var messages = await client.OrTimeout();
                Assert.Equal(2, messages.Received.Count);

                // First message: 1 byte, endOfMessage false
                Assert.Single(messages.Received[0].Buffer);
                Assert.Equal(1, messages.Received[0].Buffer[0]);
                Assert.False(messages.Received[0].EndOfMessage);

                // Second message: 1 byte, endOfMessage true
                Assert.Single(messages.Received[1].Buffer);
                Assert.Equal(15, messages.Received[1].Buffer[0]);
                Assert.True(messages.Received[1].EndOfMessage);
            }
        }
    }
}
