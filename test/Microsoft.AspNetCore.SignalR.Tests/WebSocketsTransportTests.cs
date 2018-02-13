// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging.Testing;
using Moq;
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
                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                var webSocketsTransport = new WebSocketsTransport(httpOptions: null, loggerFactory: loggerFactory);
                await webSocketsTransport.StartAsync(new Uri(_serverFixture.WebSocketsUrl + "/echo"), pair.Application,
                    TransferMode.Binary, connection: Mock.Of<IConnection>()).OrTimeout();
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
                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                var webSocketsTransport = new WebSocketsTransport(httpOptions: null, loggerFactory: loggerFactory);
                await webSocketsTransport.StartAsync(new Uri(_serverFixture.WebSocketsUrl + "/echo"), pair.Application,
                    TransferMode.Binary, connection: Mock.Of<IConnection>());
                pair.Transport.Output.Complete();
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
                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                var webSocketsTransport = new WebSocketsTransport(httpOptions: null, loggerFactory: loggerFactory);
                await webSocketsTransport.StartAsync(new Uri(_serverFixture.WebSocketsUrl + "/echo"), pair.Application, transferMode, connection: Mock.Of<IConnection>());

                await pair.Transport.Output.WriteAsync(new byte[] { 0x42 });

                // The echo endpoint closes the connection immediately after sending response which should stop the transport
                await webSocketsTransport.Running.OrTimeout();

                Assert.True(pair.Transport.Input.TryRead(out var result));
                Assert.Equal(new byte[] { 0x42 }, result.Buffer.ToArray());
                pair.Transport.Input.AdvanceTo(result.Buffer.End);
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
                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                var webSocketsTransport = new WebSocketsTransport(httpOptions: null, loggerFactory: loggerFactory);

                Assert.Null(webSocketsTransport.Mode);
                await webSocketsTransport.StartAsync(new Uri(_serverFixture.WebSocketsUrl + "/echo"), pair.Application,
                    transferMode, connection: Mock.Of<IConnection>()).OrTimeout();
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
                var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
                var webSocketsTransport = new WebSocketsTransport(httpOptions: null, loggerFactory: loggerFactory);
                var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                    webSocketsTransport.StartAsync(new Uri("http://fakeuri.org"), pair.Application, TransferMode.Text | TransferMode.Binary, connection: Mock.Of<IConnection>()));

                Assert.Contains("Invalid transfer mode.", exception.Message);
                Assert.Equal("requestedTransferMode", exception.ParamName);
            }
        }
    }
}
