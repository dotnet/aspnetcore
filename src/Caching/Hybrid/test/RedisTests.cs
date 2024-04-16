// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid.Internal;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;

public class RedisFixture : IDisposable
{
    private IConnectionMultiplexer? muxer;
    private Task<IConnectionMultiplexer?>? sharedConnect;
    public Task<IConnectionMultiplexer?> ConnectAsync() => sharedConnect ??= DoConnectAsync();

    public void Dispose() => muxer?.Dispose();

    async Task<IConnectionMultiplexer?> DoConnectAsync()
    {
        try
        {
            muxer = await ConnectionMultiplexer.ConnectAsync("127.0.0.1:6379");
            await muxer.GetDatabase().PingAsync();
            return muxer;
        }
        catch
        {
            return null;
        }
    }
}
public class RedisTests(RedisFixture fixture, ITestOutputHelper log) : IClassFixture<RedisFixture>
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BasicUsage(bool useBuffers)
    {
        var redis = await fixture.ConnectAsync();
        if (redis is null)
        {
            log.WriteLine("Redis is not available");
            return; // inconclusive
        }
        log.WriteLine("Redis is available");
        var services = new ServiceCollection();
        services.AddStackExchangeRedisCache(options =>
        {
            options.ConnectionMultiplexerFactory = () => Task.FromResult(redis);
        });
        services.AddHybridCache();
        var provider = services.BuildServiceProvider(); // not "using" - that will tear down our redis; use the fixture for that

        var cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());
        Assert.IsAssignableFrom<RedisCache>(cache.BackendCache);

        if (!useBuffers) // force byte[] mode
        {
            cache.DebugRemoveFeatures(DefaultHybridCache.CacheFeatures.BackendBuffers);
        }
        log.WriteLine($"features: {cache.GetFeatures()}");

        var key = Me();
        await redis.GetDatabase().KeyDeleteAsync(key); // start from known state
        Assert.False(await redis.GetDatabase().KeyExistsAsync(key));

        int count = 0;
        for (int i = 0; i < 10; i++)
        {
            await cache.GetOrCreateAsync<Guid>(key, _ => {
                Interlocked.Increment(ref count);
                return new(Guid.NewGuid());
            });
        }
        Assert.Equal(1, count);

        await Task.Delay(500); // the L2 write continues in the background; give it a chance

        var ttl = await redis.GetDatabase().KeyTimeToLiveAsync(key);
        log.WriteLine($"ttl: {ttl}");
        Assert.NotNull(ttl);
    }

    private static string Me([CallerMemberName] string caller = "") => caller;
}
