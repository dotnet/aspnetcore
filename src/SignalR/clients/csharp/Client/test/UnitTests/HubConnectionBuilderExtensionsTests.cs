// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests;

public class HubConnectionBuilderExtensionsTests
{
    [Fact]
    public void WithHttpConnectionSetsUrl()
    {
        var connectionBuilder = new HubConnectionBuilder();
        connectionBuilder.WithUrl("http://tempuri.org");

        var serviceProvider = connectionBuilder.Services.BuildServiceProvider();

        var value = serviceProvider.GetService<IOptions<HttpConnectionOptions>>().Value;

        Assert.Equal(new Uri("http://tempuri.org"), value.Url);
    }

    [Fact]
    public void WithHttpConnectionSetsTransport()
    {
        var connectionBuilder = new HubConnectionBuilder();
        connectionBuilder.WithUrl("http://tempuri.org", HttpTransportType.LongPolling);

        var serviceProvider = connectionBuilder.Services.BuildServiceProvider();

        var value = serviceProvider.GetService<IOptions<HttpConnectionOptions>>().Value;

        Assert.Equal(HttpTransportType.LongPolling, value.Transports);
    }

    [Fact]
    public void WithUrlUsingUriSetsTransport()
    {
        var connectionBuilder = new HubConnectionBuilder();
        var uri = new Uri("http://tempuri.org");
        connectionBuilder.WithUrl(uri, HttpTransportType.LongPolling);

        var serviceProvider = connectionBuilder.Services.BuildServiceProvider();

        var value = serviceProvider.GetService<IOptions<HttpConnectionOptions>>().Value;

        Assert.Equal(HttpTransportType.LongPolling, value.Transports);
    }

    [Fact]
    public void WithUrlUsingUriHttpConnectionCallsConfigure()
    {
        var proxy = Mock.Of<IWebProxy>();

        var connectionBuilder = new HubConnectionBuilder();
        var uri = new Uri("http://tempuri.org");
        connectionBuilder.WithUrl(uri, HttpTransportType.LongPolling, options => { options.Proxy = proxy; });

        var serviceProvider = connectionBuilder.Services.BuildServiceProvider();

        var value = serviceProvider.GetService<IOptions<HttpConnectionOptions>>().Value;

        Assert.Same(proxy, value.Proxy);
    }

    [Fact]
    public void WithHttpConnectionCallsConfigure()
    {
        var proxy = Mock.Of<IWebProxy>();

        var connectionBuilder = new HubConnectionBuilder();
        connectionBuilder.WithUrl("http://tempuri.org", options => { options.Proxy = proxy; });

        var serviceProvider = connectionBuilder.Services.BuildServiceProvider();

        var value = serviceProvider.GetService<IOptions<HttpConnectionOptions>>().Value;

        Assert.Same(proxy, value.Proxy);
    }

    [Fact]
    public void DefaultLoggerFactoryExists()
    {
        var connectionBuilder = new HubConnectionBuilder();
        var serviceProvider = connectionBuilder.Services.BuildServiceProvider();

        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        Assert.NotNull(loggerFactory);
    }

    [Fact]
    public void AddJsonProtocolAddsProtocol()
    {
        var connectionBuilder = new HubConnectionBuilder();
        connectionBuilder.AddNewtonsoftJsonProtocol();

        var serviceProvider = connectionBuilder.Services.BuildServiceProvider();

        var resolvedHubProtocol = serviceProvider.GetService<IHubProtocol>();

        Assert.IsType<NewtonsoftJsonHubProtocol>(resolvedHubProtocol);
    }

    [Fact]
    public void AddMessagePackProtocolAddsProtocol()
    {
        var connectionBuilder = new HubConnectionBuilder();
        connectionBuilder.AddMessagePackProtocol();

        var serviceProvider = connectionBuilder.Services.BuildServiceProvider();

        var resolvedHubProtocol = serviceProvider.GetService<IHubProtocol>();

        Assert.IsType<MessagePackHubProtocol>(resolvedHubProtocol);
    }
}
