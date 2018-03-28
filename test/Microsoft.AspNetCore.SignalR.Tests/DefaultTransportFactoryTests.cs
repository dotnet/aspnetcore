// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using Microsoft.AspNetCore.Sockets;
using Microsoft.AspNetCore.Sockets.Client;
using Microsoft.AspNetCore.Sockets.Client.Internal;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class DefaultTransportFactoryTests
    {
        [Theory]
        [InlineData((TransportType)0)]
        [InlineData(TransportType.All + 1)]
        public void DefaultTransportFactoryCannotBeCreatedWithInvalidTransportType(TransportType transportType)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new DefaultTransportFactory(transportType, new LoggerFactory(), new HttpClient(), httpOptions: null));
        }

        [Theory]
        [InlineData(TransportType.All)]
        [InlineData(TransportType.LongPolling)]
        [InlineData(TransportType.ServerSentEvents)]
        [InlineData(TransportType.LongPolling | TransportType.WebSockets)]
        [InlineData(TransportType.ServerSentEvents | TransportType.WebSockets)]
        public void DefaultTransportFactoryCannotBeCreatedWithoutHttpClient(TransportType transportType)
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new DefaultTransportFactory(transportType, new LoggerFactory(), httpClient: null, httpOptions: null));

            Assert.Equal("httpClient", exception.ParamName);
        }

        [Fact]
        public void DefaultTransportFactoryCanBeCreatedWithoutHttpClientIfWebSocketsTransportRequestedExplicitly()
        {
            new DefaultTransportFactory(TransportType.WebSockets, new LoggerFactory(), httpClient: null, httpOptions: null);
        }

        [ConditionalTheory]
        [InlineData(TransportType.WebSockets, typeof(WebSocketsTransport))]
        [InlineData(TransportType.ServerSentEvents, typeof(ServerSentEventsTransport))]
        [InlineData(TransportType.LongPolling, typeof(LongPollingTransport))]
        [WebSocketsSupportedCondition]
        public void DefaultTransportFactoryCreatesRequestedTransportIfAvailable(TransportType requestedTransport, Type expectedTransportType)
        {
            var transportFactory = new DefaultTransportFactory(requestedTransport, loggerFactory: null, httpClient: new HttpClient(), httpOptions: null);
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
                new DefaultTransportFactory(requestedTransport, loggerFactory: null, httpClient: new HttpClient(), httpOptions: null);
            var ex = Assert.Throws<InvalidOperationException>(
                () => transportFactory.CreateTransport(~requestedTransport));

            Assert.Equal("No requested transports available on the server.", ex.Message);
        }

        [ConditionalFact]
        [WebSocketsSupportedCondition]
        public void DefaultTransportFactoryCreatesWebSocketsTransportIfAvailable()
        {
            Assert.IsType<WebSocketsTransport>(
                new DefaultTransportFactory(TransportType.All, loggerFactory: null, httpClient: new HttpClient(), httpOptions: null)
                    .CreateTransport(TransportType.All));
        }

        [Theory]
        [InlineData(TransportType.All, typeof(ServerSentEventsTransport))]
        [InlineData(TransportType.ServerSentEvents, typeof(ServerSentEventsTransport))]
        [InlineData(TransportType.LongPolling, typeof(LongPollingTransport))]
        public void DefaultTransportFactoryCreatesRequestedTransportIfAvailable_Win7(TransportType requestedTransport, Type expectedTransportType)
        {
            if (!TestHelpers.IsWebSocketsSupported())
            {
                var transportFactory = new DefaultTransportFactory(requestedTransport, loggerFactory: null, httpClient: new HttpClient(), httpOptions: null);
                Assert.IsType(expectedTransportType,
                    transportFactory.CreateTransport(TransportType.All));
            }
        }

        [Theory]
        [InlineData(TransportType.WebSockets)]
        public void DefaultTransportFactoryThrowsIfItCannotCreateRequestedTransport_Win7(TransportType requestedTransport)
        {
            if (!TestHelpers.IsWebSocketsSupported())
            {
                var transportFactory =
                    new DefaultTransportFactory(requestedTransport, loggerFactory: null, httpClient: new HttpClient(), httpOptions: null);
                var ex = Assert.Throws<InvalidOperationException>(
                    () => transportFactory.CreateTransport(TransportType.All));

                Assert.Equal("No requested transports available on the server.", ex.Message);
            }
        }
    }
}
