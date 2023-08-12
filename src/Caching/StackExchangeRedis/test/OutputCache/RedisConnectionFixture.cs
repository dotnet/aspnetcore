// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET7_0_OR_GREATER

using System;
using StackExchange.Redis;
using Xunit;

namespace Microsoft.Extensions.Caching.StackExchangeRedis;

public class RedisConnectionFixture : IDisposable
{
    private readonly ConnectionMultiplexer _muxer;
    public RedisConnectionFixture()
    {
        var options = new RedisCacheOptions
        {
            Configuration = "127.0.0.1:6379", // TODO: CI test config here
        }.GetConfiguredOptions("CI test");
        _muxer = ConnectionMultiplexer.Connect(options);
    }

    public IDatabase Database => _muxer.GetDatabase();

    public IConnectionMultiplexer Connection => _muxer;

    public void Dispose() => _muxer.Dispose();
}

#endif
