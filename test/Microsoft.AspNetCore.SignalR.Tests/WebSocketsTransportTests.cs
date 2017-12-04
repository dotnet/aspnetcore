// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.AspNetCore.Sockets.Internal;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    [Collection(EndToEndTestsCollection.Name)]
    public class WebSocketsTransportTests : LoggedTest
    {
        private readonly ServerFixture<Startup> _serverFixture;

        public WebSocketsTransportTests(ServerFixture<Startup> serverFixture, ITestOutputHelper output) : base(output)
        {
            if (serverFixture == null)
            {
                throw new ArgumentNullException(nameof(serverFixture));
            }

            _serverFixture = serverFixture;
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win2008R2, SkipReason = "No WebSockets Client for this platform")]
        public async Task WebSocketsTransportStopsSendAndReceiveLoopsWhenTransportIsStopped()
        {
            using (StartLog(out var loggerFactory))
            {
                var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                var transportToConnection = Channel.CreateUnbounded<byte[]>();
                var channelConnection = new ChannelConnection<SendMessage, byte[]>(connectionToTransport, transportToConnection);

                var webSocketsTransport = new WebSocketsTransport(httpOptions: null, loggerFactory: loggerFactory);
                await webSocketsTransport.StartAsync(new Uri(_serverFixture.WebSocketsUrl + "/echo"), channelConnection,
                    TransferMode.Binary, connectionId: string.Empty).OrTimeout();
                await webSocketsTransport.StopAsync().OrTimeout();
                await webSocketsTransport.Running.OrTimeout();
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win2008R2, SkipReason = "No WebSockets Client for this platform")]
        public async Task WebSocketsTransportStopsWhenConnectionChannelClosed()
        {
            using (StartLog(out var loggerFactory))
            {
                var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                var transportToConnection = Channel.CreateUnbounded<byte[]>();
                var channelConnection = new ChannelConnection<SendMessage, byte[]>(connectionToTransport, transportToConnection);

                var webSocketsTransport = new WebSocketsTransport(httpOptions: null, loggerFactory: loggerFactory);
                await webSocketsTransport.StartAsync(new Uri(_serverFixture.WebSocketsUrl + "/echo"), channelConnection,
                    TransferMode.Binary, connectionId: string.Empty);
                connectionToTransport.Writer.TryComplete();
                await webSocketsTransport.Running.OrTimeout(TimeSpan.FromSeconds(10));
            }
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win2008R2, SkipReason = "No WebSockets Client for this platform")]
        [InlineData(TransferMode.Text)]
        [InlineData(TransferMode.Binary)]
        public async Task WebSocketsTransportStopsWhenConnectionClosedByTheServer(TransferMode transferMode)
        {
            using (StartLog(out var loggerFactory))
            {
                var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                var transportToConnection = Channel.CreateUnbounded<byte[]>();
                var channelConnection = new ChannelConnection<SendMessage, byte[]>(connectionToTransport, transportToConnection);

                var webSocketsTransport = new WebSocketsTransport(httpOptions: null, loggerFactory: loggerFactory);
                await webSocketsTransport.StartAsync(new Uri(_serverFixture.WebSocketsUrl + "/echo"), channelConnection, transferMode, connectionId: string.Empty);

                var sendTcs = new TaskCompletionSource<object>();
                connectionToTransport.Writer.TryWrite(new SendMessage(new byte[] { 0x42 }, sendTcs));
                try
                {
                    await sendTcs.Task;
                }
                catch (OperationCanceledException)
                {
                    // Because the server and client are run in the same process there is a race where websocket.SendAsync
                    // can send a message but before returning be suspended allowing the server to run the EchoEndpoint and
                    // send a close frame which triggers a cancellation token on the client and cancels the websocket.SendAsync.
                    // Our solution to this is to just catch OperationCanceledException from the sent message if the race happens
                    // because we know the send went through, and its safe to check the response.
                }

                // The echo endpoint closes the connection immediately after sending response which should stop the transport
                await webSocketsTransport.Running.OrTimeout();

                Assert.True(transportToConnection.Reader.TryRead(out var buffer));
                Assert.Equal(new byte[] { 0x42 }, buffer);
            }
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win2008R2, SkipReason = "No WebSockets Client for this platform")]
        [InlineData(TransferMode.Text)]
        [InlineData(TransferMode.Binary)]
        public async Task WebSocketsTransportSetsTransferMode(TransferMode transferMode)
        {
            using (StartLog(out var loggerFactory))
            {
                var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                var transportToConnection = Channel.CreateUnbounded<byte[]>();
                var channelConnection = new ChannelConnection<SendMessage, byte[]>(connectionToTransport, transportToConnection);

                var webSocketsTransport = new WebSocketsTransport(httpOptions: null, loggerFactory: loggerFactory);

                Assert.Null(webSocketsTransport.Mode);
                await webSocketsTransport.StartAsync(new Uri(_serverFixture.WebSocketsUrl + "/echo"), channelConnection,
                    transferMode, connectionId: string.Empty).OrTimeout();
                Assert.Equal(transferMode, webSocketsTransport.Mode);

                await webSocketsTransport.StopAsync().OrTimeout();
                await webSocketsTransport.Running.OrTimeout();
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win2008R2, SkipReason = "No WebSockets Client for this platform")]
        public async Task WebSocketsTransportThrowsForInvalidTransferMode()
        {
            using (StartLog(out var loggerFactory))
            {
                var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                var transportToConnection = Channel.CreateUnbounded<byte[]>();
                var channelConnection = new ChannelConnection<SendMessage, byte[]>(connectionToTransport, transportToConnection);

                var webSocketsTransport = new WebSocketsTransport(httpOptions: null, loggerFactory: loggerFactory);
                var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                    webSocketsTransport.StartAsync(new Uri("http://fakeuri.org"), channelConnection, TransferMode.Text | TransferMode.Binary, connectionId: string.Empty));

                Assert.Contains("Invalid transfer mode.", exception.Message);
                Assert.Equal("requestedTransferMode", exception.ParamName);
            }
        }
    }
}
