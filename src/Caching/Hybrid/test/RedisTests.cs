// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Hybrid.Internal;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;

public sealed class RedisFixture : IDisposable
{
    private ConnectionMultiplexer? _muxer;
    private Task<IConnectionMultiplexer?>? _sharedConnect;
    public Task<IConnectionMultiplexer?> ConnectAsync() => _sharedConnect ??= DoConnectAsync();

    public void Dispose() => _muxer?.Dispose();

    async Task<IConnectionMultiplexer?> DoConnectAsync()
    {
        try
        {
            _muxer = await ConnectionMultiplexer.ConnectAsync("127.0.0.1:6379");
            await _muxer.GetDatabase().PingAsync();
            return _muxer;
        }
        catch
        {
            return null;
        }
    }
}
public class RedisTests : DistributedCacheTests, IClassFixture<RedisFixture>
{
    private readonly RedisFixture _fixture;
    public RedisTests(RedisFixture fixture, ITestOutputHelper log) : base(log) => _fixture = fixture;

    protected override bool CustomClockSupported => false;

    protected override async ValueTask ConfigureAsync(IServiceCollection services)
    {
        var redis = await _fixture.ConnectAsync();
        if (redis is null)
        {
            Log.WriteLine("Redis is not available");
            return; // inconclusive
        }
        Log.WriteLine("Redis is available");
        services.AddSingleton<IConnectionMultiplexer>(redis);
        services.AddStackExchangeRedisCache(options =>
        {
            options.ConnectionMultiplexerFactory = () => Task.FromResult(redis);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BasicUsage(bool useBuffers)
    {
        var services = new ServiceCollection();
        await ConfigureAsync(services);
        services.AddHybridCache();
        var provider = services.BuildServiceProvider(); // not "using" - that will tear down our redis; use the fixture for that

        var cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());
        if (cache.BackendCache is null)
        {
            Log.WriteLine("Backend cache not available; inconclusive");
            return;
        }
        Assert.IsAssignableFrom<RedisCache>(cache.BackendCache);

        if (!useBuffers) // force byte[] mode
        {
            cache.DebugRemoveFeatures(DefaultHybridCache.CacheFeatures.BackendBuffers);
        }
        Log.WriteLine($"features: {cache.GetFeatures()}");

        var key = Me();
        var redis = provider.GetRequiredService<IConnectionMultiplexer>();
        await redis.GetDatabase().KeyDeleteAsync(key); // start from known state
        Assert.False(await redis.GetDatabase().KeyExistsAsync(key));

        var count = 0;
        for (var i = 0; i < 10; i++)
        {
            await cache.GetOrCreateAsync<Guid>(key, _ =>
            {
                Interlocked.Increment(ref count);
                return new(Guid.NewGuid());
            });
        }
        Assert.Equal(1, count);

        await Task.Delay(500); // the L2 write continues in the background; give it a chance

        var ttl = await redis.GetDatabase().KeyTimeToLiveAsync(key);
        Log.WriteLine($"ttl: {ttl}");
        Assert.NotNull(ttl);
    }
}
