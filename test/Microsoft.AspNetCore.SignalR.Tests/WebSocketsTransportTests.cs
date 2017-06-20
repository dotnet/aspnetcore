// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.SignalR.Tests.Common;
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
        private readonly ServerFixture _serverFixture;

        public WebSocketsTransportTests(ServerFixture serverFixture, ITestOutputHelper output) : base(output)
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
                await webSocketsTransport.StartAsync(new Uri(_serverFixture.WebSocketsUrl + "/echo"), channelConnection).OrTimeout();
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
                await webSocketsTransport.StartAsync(new Uri(_serverFixture.WebSocketsUrl + "/echo"), channelConnection);
                connectionToTransport.Out.TryComplete();
                await webSocketsTransport.Running.OrTimeout();
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win2008R2, SkipReason = "No WebSockets Client for this platform")]
        public async Task WebSocketsTransportStopsWhenConnectionClosedByTheServer()
        {
            using (StartLog(out var loggerFactory))
            {
                var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
                var transportToConnection = Channel.CreateUnbounded<byte[]>();
                var channelConnection = new ChannelConnection<SendMessage, byte[]>(connectionToTransport, transportToConnection);

                var webSocketsTransport = new WebSocketsTransport(loggerFactory);
                await webSocketsTransport.StartAsync(new Uri(_serverFixture.WebSocketsUrl + "/echo"), channelConnection);

                var sendTcs = new TaskCompletionSource<object>();
                connectionToTransport.Out.TryWrite(new SendMessage(new byte[] { 0x42 }, sendTcs));
                await sendTcs.Task;
                // The echo endpoint close the connection immediately after sending response which should stop the transport
                await webSocketsTransport.Running.OrTimeout();

                Assert.True(transportToConnection.In.TryRead(out var buffer));
                Assert.Equal(new byte[] { 0x42 }, buffer);
            }
        }
    }
}
