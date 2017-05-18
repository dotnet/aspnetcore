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
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    [Collection(EndToEndTestsCollection.Name)]
    public class WebSocketsTransportTests
    {
        private readonly ServerFixture _serverFixture;

        public WebSocketsTransportTests(ServerFixture serverFixture)
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
            var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
            var transportToConnection = Channel.CreateUnbounded<Message>();
            var channelConnection = new ChannelConnection<SendMessage, Message>(connectionToTransport, transportToConnection);

            var webSocketsTransport = new WebSocketsTransport();
            await webSocketsTransport.StartAsync(new Uri(_serverFixture.WebSocketsUrl + "/echo"), channelConnection);
            await webSocketsTransport.StopAsync();
            await webSocketsTransport.Running.OrTimeout();
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win2008R2, SkipReason = "No WebSockets Client for this platform")]
        public async Task WebSocketsTransportStopsWhenConnectionChannelClosed()
        {
            var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
            var transportToConnection = Channel.CreateUnbounded<Message>();
            var channelConnection = new ChannelConnection<SendMessage, Message>(connectionToTransport, transportToConnection);

            var webSocketsTransport = new WebSocketsTransport();
            await webSocketsTransport.StartAsync(new Uri(_serverFixture.WebSocketsUrl + "/echo"), channelConnection);
            connectionToTransport.Out.TryComplete();
            await webSocketsTransport.Running.OrTimeout();
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win2008R2, SkipReason = "No WebSockets Client for this platform")]
        public async Task WebSocketsTransportStopsWhenConnectionClosedByTheServer()
        {
            var connectionToTransport = Channel.CreateUnbounded<SendMessage>();
            var transportToConnection = Channel.CreateUnbounded<Message>();
            var channelConnection = new ChannelConnection<SendMessage, Message>(connectionToTransport, transportToConnection);

            var webSocketsTransport = new WebSocketsTransport();
            await webSocketsTransport.StartAsync(new Uri(_serverFixture.WebSocketsUrl + "/echo"), channelConnection);

            var sendTcs = new TaskCompletionSource<object>();
            connectionToTransport.Out.TryWrite(new SendMessage(new byte[] { 0x42 }, MessageType.Binary, sendTcs));
            await sendTcs.Task;
            // The echo endpoint close the connection immediately after sending response which should stop the transport
            await webSocketsTransport.Running.OrTimeout();

            Assert.True(transportToConnection.In.TryRead(out var message));
            Assert.Equal(new byte[] { 0x42 }, message.Payload);
        }
    }
}
