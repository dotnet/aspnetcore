// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.AspNetCore.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Moq;
using Xunit;
using TransportType = Microsoft.AspNetCore.Sockets.TransportType;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public class HubConnectionBuilderExtensionsTests
    {
        [Fact]
        public void WithProxyRegistersGivenProxy()
        {
            var connectionBuilder = new HubConnectionBuilder();
            var proxy = Mock.Of<IWebProxy>();
            connectionBuilder.WithProxy(proxy);
            Assert.Same(proxy, connectionBuilder.GetProxy());
        }

        [Fact]
        public void WithCredentialsRegistersGivenCredentials()
        {
            var connectionBuilder = new HubConnectionBuilder();
            var credentials = Mock.Of<ICredentials>();
            connectionBuilder.WithCredentials(credentials);
            Assert.Same(credentials, connectionBuilder.GetCredentials());
        }

        [Fact]
        public void WithUseDefaultCredentialsRegistersGivenUseDefaultCredentials()
        {
            var connectionBuilder = new HubConnectionBuilder();
            var useDefaultCredentials = true;
            connectionBuilder.WithUseDefaultCredentials(useDefaultCredentials);
            Assert.Equal(useDefaultCredentials, connectionBuilder.GetUseDefaultCredentials());
        }

        [Fact]
        public void WithClientCertificateRegistersGivenClientCertificate()
        {
            var connectionBuilder = new HubConnectionBuilder();
            var certificate = new X509Certificate();
            connectionBuilder.WithClientCertificate(certificate);
            Assert.Contains(certificate, connectionBuilder.GetClientCertificates().Cast<X509Certificate>());
        }

        [Fact]
        public void WithCookieRegistersGivenCookie()
        {
            var connectionBuilder = new HubConnectionBuilder();
            var cookie = new Cookie("Name!", "Value!", string.Empty, "www.contoso.com");
            connectionBuilder.WithCookie(cookie);
            Assert.Equal(1, connectionBuilder.GetCookies().Count);
        }

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
            var messageHandler = new Func<HttpMessageHandler, HttpMessageHandler>(httpMessageHandler => default);

            var connectionBuilder = new HubConnectionBuilder();
            connectionBuilder.WithMessageHandler(messageHandler);
            Assert.Same(messageHandler, connectionBuilder.GetMessageHandler());
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
