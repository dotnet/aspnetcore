// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class ListenOptionsTests
{
    [Fact]
    public void ProtocolsDefault()
    {
        var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));
        Assert.Equal(ListenOptions.DefaultHttpProtocols, listenOptions.Protocols);
    }

    [Fact]
    public void LocalHostListenOptionsClonesConnectionMiddleware()
    {
        var localhostListenOptions = new LocalhostListenOptions(1004);
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        localhostListenOptions.KestrelServerOptions = new KestrelServerOptions
        {
            ApplicationServices = serviceProvider
        };
        var middlewareRan = false;
        localhostListenOptions.Use(next =>
        {
            middlewareRan = true;
            return context => Task.CompletedTask;
        });

        var clone = localhostListenOptions.Clone(IPAddress.IPv6Loopback);
        var app = clone.Build();

        // Execute the delegate
        app(null);

        Assert.True(middlewareRan);
        Assert.NotNull(clone.KestrelServerOptions);
        Assert.NotNull(serviceProvider);
        Assert.Same(serviceProvider, clone.ApplicationServices);
    }

    [Fact]
    public void ClonePreservesProtocolsSetExplicitly()
    {
        var localhostListenOptions = new LocalhostListenOptions(1004);
        Assert.False(localhostListenOptions.ProtocolsSetExplicitly);

        var clone1 = localhostListenOptions.Clone(IPAddress.IPv6Loopback);
        Assert.False(clone1.ProtocolsSetExplicitly);
        Assert.Equal(localhostListenOptions.Protocols, clone1.Protocols);

        localhostListenOptions.Protocols = HttpProtocols.Http1;
        Assert.True(localhostListenOptions.ProtocolsSetExplicitly);

        var clone2 = localhostListenOptions.Clone(IPAddress.IPv6Loopback);
        Assert.True(clone2.ProtocolsSetExplicitly);
        Assert.Equal(localhostListenOptions.Protocols, clone2.Protocols);
    }

    [Fact]
    public void ListenOptionsSupportsAnyEndPoint()
    {
        var listenOptions = new ListenOptions(new UriEndPoint(new Uri("http://127.0.0.1:5555")));
        Assert.IsType<UriEndPoint>(listenOptions.EndPoint);
        Assert.Equal("http://127.0.0.1:5555/", ((UriEndPoint)listenOptions.EndPoint).Uri.ToString());
    }
}
