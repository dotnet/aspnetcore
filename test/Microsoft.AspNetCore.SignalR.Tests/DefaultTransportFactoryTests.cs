// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class DefaultTransportFactoryTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(TransportType.All + 1)]
        public void DefaultTransportFactoryCannotBeCreatedWithInvalidTransportType(TransportType transportType)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new DefaultTransportFactory(transportType, new LoggerFactory(), new HttpClient()));
        }

        [Fact]
        public void DefaultTransportFactoryCannotBeCreatedWithoutHttpClient()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new DefaultTransportFactory(TransportType.All, new LoggerFactory(), httpClient: null));

            Assert.Equal(exception.ParamName, "httpClient");
        }

        [Theory]
        [InlineData(TransportType.WebSockets, typeof(WebSocketsTransport))]
        [InlineData(TransportType.LongPolling, typeof(LongPollingTransport))]
        public void DefaultTransportFactoryCreatesRequestedTransportIfAvailable(TransportType requestedTransport, Type expectedTransportType)
        {
            var transportFactory = new DefaultTransportFactory(requestedTransport, loggerFactory: null, httpClient: new HttpClient());
            Assert.IsType(expectedTransportType,
                transportFactory.CreateTransport(TransportType.All));
        }

        [Theory]
        [InlineData(TransportType.WebSockets)]
        [InlineData(TransportType.ServerSentEvents)]
        [InlineData(TransportType.LongPolling)]
        [InlineData(TransportType.All)]
        public void DefaultTransportFactoryThrowsIfItCannotCreateRequestedTransport(TransportType requestedTransport)
        {
            var transportFactory =
                new DefaultTransportFactory(requestedTransport, loggerFactory: null, httpClient: new HttpClient());
            var ex = Assert.Throws<InvalidOperationException>(
                () => transportFactory.CreateTransport(~requestedTransport));

            Assert.Equal("No requested transports available on the server.", ex.Message);
        }

        [Fact]
        public void DefaultTransportFactoryCreatesWebSocketsTransportIfAvailable()
        {
            Assert.IsType<WebSocketsTransport>(
                new DefaultTransportFactory(TransportType.All, loggerFactory: null, httpClient: new HttpClient())
                    .CreateTransport(TransportType.All));
        }
    }
}
