// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Tests;

public class RedisDependencyInjectionExtensionsTests
{
    // No need to go too deep with these tests, or we're just testing StackExchange.Redis again :). It's the one doing the parsing.
    [Theory]
    [InlineData("testredis.example.com", "testredis.example.com", 0, null, false)]
    [InlineData("testredis.example.com:6380,ssl=True", "testredis.example.com", 6380, null, true)]
    [InlineData("testredis.example.com:6380,password=hunter2,ssl=True", "testredis.example.com", 6380, "hunter2", true)]
    public void AddRedisWithConnectionStringProperlyParsesOptions(string connectionString, string host, int port, string password, bool useSsl)
    {
        var services = new ServiceCollection();
        services.AddSignalR().AddStackExchangeRedis(connectionString);
        var provider = services.BuildServiceProvider();

        var options = provider.GetService<IOptions<RedisOptions>>();
        Assert.NotNull(options.Value);
        Assert.NotNull(options.Value.Configuration);
        Assert.Equal(password, options.Value.Configuration.Password);
        Assert.Collection(options.Value.Configuration.EndPoints,
            endpoint =>
            {
                var dnsEndpoint = Assert.IsType<DnsEndPoint>(endpoint);
                Assert.Equal(host, dnsEndpoint.Host);
                Assert.Equal(port, dnsEndpoint.Port);
            });
        Assert.Equal(useSsl, options.Value.Configuration.Ssl);
    }
}
