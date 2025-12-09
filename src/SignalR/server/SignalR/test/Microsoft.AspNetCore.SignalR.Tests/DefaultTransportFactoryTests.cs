// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests;

public class DefaultTransportFactoryTests
{
    private const HttpTransportType AllTransportTypes = HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling;

    [Theory]
    [InlineData(HttpTransportType.None)]
    [InlineData((HttpTransportType)int.MaxValue)]
    public void DefaultTransportFactoryCanBeCreatedWithNoOrUnknownTransportTypeFlags(HttpTransportType transportType)
    {
        Assert.NotNull(new DefaultTransportFactory(transportType, new LoggerFactory(), new HttpClient(), httpConnectionOptions: null, accessTokenProvider: null));
    }

    [Theory]
    [InlineData(AllTransportTypes)]
    [InlineData(HttpTransportType.LongPolling)]
    [InlineData(HttpTransportType.ServerSentEvents)]
    [InlineData(HttpTransportType.LongPolling | HttpTransportType.WebSockets)]
    [InlineData(HttpTransportType.ServerSentEvents | HttpTransportType.WebSockets)]
    public void DefaultTransportFactoryCannotBeCreatedWithoutHttpClient(HttpTransportType transportType)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new DefaultTransportFactory(transportType, new LoggerFactory(), httpClient: null, httpConnectionOptions: null, accessTokenProvider: null));

        Assert.Equal("httpClient", exception.ParamName);
    }

    [Fact]
    public void DefaultTransportFactoryCanBeCreatedWithoutHttpClientIfWebSocketsTransportRequestedExplicitly()
    {
        new DefaultTransportFactory(HttpTransportType.WebSockets, new LoggerFactory(), httpClient: null, httpConnectionOptions: null, accessTokenProvider: null);
    }

    [ConditionalTheory]
    [InlineData(HttpTransportType.WebSockets, typeof(WebSocketsTransport))]
    [InlineData(HttpTransportType.ServerSentEvents, typeof(ServerSentEventsTransport))]
    [InlineData(HttpTransportType.LongPolling, typeof(LongPollingTransport))]
    [WebSocketsSupportedCondition]
    public void DefaultTransportFactoryCreatesRequestedTransportIfAvailable(HttpTransportType requestedTransport, Type expectedTransportType)
    {
        var transportFactory = new DefaultTransportFactory(requestedTransport, loggerFactory: null, httpClient: new HttpClient(), httpConnectionOptions: null, accessTokenProvider: null);
        Assert.IsType(expectedTransportType,
            transportFactory.CreateTransport(AllTransportTypes, useStatefulReconnect: true));
    }

    [Theory]
    [InlineData(HttpTransportType.WebSockets)]
    [InlineData(HttpTransportType.ServerSentEvents)]
    [InlineData(HttpTransportType.LongPolling)]
    [InlineData(AllTransportTypes)]
    public void DefaultTransportFactoryThrowsIfItCannotCreateRequestedTransport(HttpTransportType requestedTransport)
    {
        var transportFactory =
            new DefaultTransportFactory(requestedTransport, loggerFactory: null, httpClient: new HttpClient(), httpConnectionOptions: null, accessTokenProvider: null);
        var ex = Assert.Throws<InvalidOperationException>(
            () => transportFactory.CreateTransport(~requestedTransport, useStatefulReconnect: true));

        Assert.Equal("No requested transports available on the server.", ex.Message);
    }

    [ConditionalFact]
    [WebSocketsSupportedCondition]
    public void DefaultTransportFactoryCreatesWebSocketsTransportIfAvailable()
    {
        Assert.IsType<WebSocketsTransport>(
            new DefaultTransportFactory(AllTransportTypes, loggerFactory: null, httpClient: new HttpClient(), httpConnectionOptions: null, accessTokenProvider: null)
                .CreateTransport(AllTransportTypes, useStatefulReconnect: true));
    }

    [Theory]
    [InlineData(AllTransportTypes, typeof(ServerSentEventsTransport))]
    [InlineData(HttpTransportType.ServerSentEvents, typeof(ServerSentEventsTransport))]
    [InlineData(HttpTransportType.LongPolling, typeof(LongPollingTransport))]
    public void DefaultTransportFactoryCreatesRequestedTransportIfAvailable_Win7(HttpTransportType requestedTransport, Type expectedTransportType)
    {
        if (!TestHelpers.IsWebSocketsSupported())
        {
            var transportFactory = new DefaultTransportFactory(requestedTransport, loggerFactory: null, httpClient: new HttpClient(), httpConnectionOptions: null, accessTokenProvider: null);
            Assert.IsType(expectedTransportType,
                transportFactory.CreateTransport(AllTransportTypes, useStatefulReconnect: true));
        }
    }

    [Theory]
    [InlineData(HttpTransportType.WebSockets)]
    public void DefaultTransportFactoryThrowsIfItCannotCreateRequestedTransport_Win7(HttpTransportType requestedTransport)
    {
        if (!TestHelpers.IsWebSocketsSupported())
        {
            var transportFactory =
                new DefaultTransportFactory(requestedTransport, loggerFactory: null, httpClient: new HttpClient(), httpConnectionOptions: null, accessTokenProvider: null);
            var ex = Assert.Throws<InvalidOperationException>(
                () => transportFactory.CreateTransport(AllTransportTypes, useStatefulReconnect: true));

            Assert.Equal("No requested transports available on the server.", ex.Message);
        }
    }
}
