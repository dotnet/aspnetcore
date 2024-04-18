// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Hybrid.Internal;
using Microsoft.Extensions.DependencyInjection;
using static Microsoft.Extensions.Caching.Hybrid.Internal.DefaultHybridCache;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;

public class BufferReleaseTests // note that buffer ref-counting is only enabled for DEBUG builds; can only verify general behaviour without that
{
    static IDisposable GetDefaultCache(out DefaultHybridCache cache)
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        var provider = services.BuildServiceProvider();
        cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());
        return provider;
    }

    [Fact]
    public async Task BufferGetsReleased()
    {
        using var provider = GetDefaultCache(out var cache);
#if DEBUG
        cache.DebugGetOutstandingBuffers(flush: true);
#endif

        var key = Me();
#if DEBUG
        Assert.Equal(0, cache.DebugGetOutstandingBuffers());
#endif
        var first = await cache.GetOrCreateAsync(key, _ => GetAsync());
        Assert.NotNull(first);
#if DEBUG
        Assert.Equal(1, cache.DebugGetOutstandingBuffers());
#endif
        Assert.True(cache.DebugTryGetCacheItem(key, out var cacheItem));

        // assert that we can reserve the buffer *now* (mostly to see that it behaves differently later)
        Assert.True(cacheItem.TryReserveBuffer(out _));
        cacheItem.Release(); // for the above reserve

        var second = await cache.GetOrCreateAsync(key, _ => GetAsync(), new HybridCacheEntryOptions { Flags = HybridCacheEntryFlags.DisableUnderlyingData });
        Assert.NotNull(second);
        Assert.NotSame(first, second);

        await cache.RemoveKeyAsync(key);
        var third = await cache.GetOrCreateAsync(key, _ => GetAsync(), new HybridCacheEntryOptions { Flags = HybridCacheEntryFlags.DisableUnderlyingData });
        Assert.Null(third);

        await Task.Delay(500); // give it a moment
#if DEBUG
        Assert.Equal(0, cache.DebugGetOutstandingBuffers());
#endif
        // assert that we can *no longer* reserve this buffer, because we've already recycled it
        Assert.False(cacheItem.TryReserveBuffer(out _));

        static ValueTask<Customer> GetAsync() => new(new Customer { Id = 42, Name = "Fred" });
    }

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private static string Me([CallerMemberName] string caller = "") => caller;
}
