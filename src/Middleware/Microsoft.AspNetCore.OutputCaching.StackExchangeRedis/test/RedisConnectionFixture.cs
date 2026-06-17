// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using StackExchange.Redis;

namespace Microsoft.AspNetCore.OutputCaching.StackExchangeRedis.Tests;

public class RedisConnectionFixture : IDisposable
{
    private readonly ConnectionMultiplexer _muxer;
    public RedisConnectionFixture()
    {
        var options = new RedisOutputCacheOptions
        {
            Configuration = "127.0.0.1:6379", // TODO: CI test config here
        }.GetConfiguredOptions();
        _muxer = ConnectionMultiplexer.Connect(options);
        _muxer.AddLibraryNameSuffix("test");
    }

    public IDatabase Database => _muxer.GetDatabase();

    public IConnectionMultiplexer Connection => _muxer;

    public void Dispose() => _muxer.Dispose();
}
