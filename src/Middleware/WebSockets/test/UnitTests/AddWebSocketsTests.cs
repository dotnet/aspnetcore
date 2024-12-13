// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.WebSockets.Test;

public class AddWebSocketsTests
{
    [Fact]
    public void AddWebSocketsConfiguresOptions()
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddWebSockets(o =>
        {
            o.KeepAliveInterval = TimeSpan.FromSeconds(1000);
            o.KeepAliveTimeout = TimeSpan.FromSeconds(1234);
            o.AllowedOrigins.Add("someString");
        });

        var services = serviceCollection.BuildServiceProvider();
        var socketOptions = services.GetRequiredService<IOptions<WebSocketOptions>>().Value;

        Assert.Equal(TimeSpan.FromSeconds(1000), socketOptions.KeepAliveInterval);
        Assert.Equal(TimeSpan.FromSeconds(1234), socketOptions.KeepAliveTimeout);
        Assert.Single(socketOptions.AllowedOrigins);
        Assert.Equal("someString", socketOptions.AllowedOrigins[0]);
    }

    [Fact]
    public void ThrowsForBadOptions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WebSocketOptions() { KeepAliveTimeout = TimeSpan.FromMicroseconds(-1) });
    }
}
