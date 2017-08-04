// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.SignalR.Tests.Common;
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

                var webSocketsTransport = new WebSocketsTransport(loggerFactory);
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

                var webSocketsTransport = new WebSocketsTransport(loggerFactory);
                await webSocketsTransport.StartAsync(new Uri(_serverFixture.WebSocketsUrl + "/echo"), channelConnection,
                    TransferMode.Binary, connectionId: string.Empty);
                connectionToTransport.Out.TryComplete();
                await webSocketsTransport.Running.OrTimeout();
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

                var webSocketsTransport = new WebSocketsTransport(loggerFactory);
                await webSocketsTransport.StartAsync(new Uri(_serverFixture.WebSocketsUrl + "/echo"), channelConnection, transferMode, connectionId: string.Empty);

                var sendTcs = new TaskCompletionSource<object>();
                connectionToTransport.Out.TryWrite(new SendMessage(new byte[] { 0x42 }, sendTcs));
                await sendTcs.Task;
                // The echo endpoint closes the connection immediately after sending response which should stop the transport
                await webSocketsTransport.Running.OrTimeout();

                Assert.True(transportToConnection.In.TryRead(out var buffer));
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

                var webSocketsTransport = new WebSocketsTransport(loggerFactory);

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

                var webSocketsTransport = new WebSocketsTransport(loggerFactory);
                var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                    webSocketsTransport.StartAsync(new Uri("http://fakeuri.org"), channelConnection, TransferMode.Text | TransferMode.Binary, connectionId: string.Empty));

                Assert.Contains("Invalid transfer mode.", exception.Message);
                Assert.Equal("requestedTransferMode", exception.ParamName);
            }
        }
    }
}
