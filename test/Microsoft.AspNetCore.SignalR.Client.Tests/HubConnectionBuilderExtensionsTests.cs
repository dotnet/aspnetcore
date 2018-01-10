// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public class HubConnectionBuilderExtensionsTests
    {
        [Fact]
        public void WithHubProtocolRegistersGivenProtocol()
        {
            var connectionBuilder = new HubConnectionBuilder();
            var hubProtocol = Mock.Of<IHubProtocol>();
            connectionBuilder.WithHubProtocol(hubProtocol);
            Assert.Same(hubProtocol, connectionBuilder.GetHubProtocol());
        }

        [Fact]
        public void WithJsonProtocolRegistersJsonProtocol()
        {
            var connectionBuilder = new HubConnectionBuilder();
            connectionBuilder.WithJsonProtocol();
            Assert.IsType<JsonHubProtocol>(connectionBuilder.GetHubProtocol());
        }

        [Fact]
        public void WithMessagePackProtocolRegistersMessagePackProtocol()
        {
            var connectionBuilder = new HubConnectionBuilder();
            connectionBuilder.WithMessagePackProtocol();
            Assert.IsType<MessagePackHubProtocol>(connectionBuilder.GetHubProtocol());
        }

        [Fact]
        public void WithLoggerRegistersGivenLogger()
        {
            var connectionBuilder = new HubConnectionBuilder();
            var loggerFactory = Mock.Of<ILoggerFactory>();
            connectionBuilder.WithLoggerFactory(loggerFactory);
            Assert.Same(loggerFactory, connectionBuilder.GetLoggerFactory());
        }

        [Fact]
        public void WithConsoleLoggerRegistersConsoleLogger()
        {
            var connectionBuilder = new HubConnectionBuilder();
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            connectionBuilder.WithLoggerFactory(mockLoggerFactory.Object);
            connectionBuilder.WithConsoleLogger();
            mockLoggerFactory.Verify(f => f.AddProvider(It.IsAny<ConsoleLoggerProvider>()), Times.Once);
        }

        [Fact]
        public void WithMsgHandlerRegistersGivenMessageHandler()
        {
            var connectionBuilder = new HubConnectionBuilder();
            var msgHandler = Mock.Of<HttpMessageHandler>();
            connectionBuilder.WithMessageHandler(msgHandler);
            Assert.Same(msgHandler, connectionBuilder.GetMessageHandler());
        }

        [Theory]
        [InlineData(TransportType.All)]
        [InlineData(TransportType.WebSockets)]
        [InlineData(TransportType.ServerSentEvents)]
        [InlineData(TransportType.LongPolling)]
        public void WithTransportRegistersGivenTransportType(TransportType transportType)
        {
            var connectionBuilder = new HubConnectionBuilder();
            connectionBuilder.WithTransport(transportType);
            Assert.Equal(transportType, connectionBuilder.GetTransport());
        }
    }
}
