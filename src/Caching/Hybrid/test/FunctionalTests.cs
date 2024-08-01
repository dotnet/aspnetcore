// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Hybrid.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;
public class FunctionalTests
{
    static ServiceProvider GetDefaultCache(out DefaultHybridCache cache, Action<ServiceCollection>? config = null)
    {
        var services = new ServiceCollection();
        config?.Invoke(services);
        services.AddHybridCache();
        var provider = services.BuildServiceProvider();
        cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());
        return provider;
    }

    [Fact]
    public async Task RemoveSingleKey()
    {
        using var provider = GetDefaultCache(out var cache);
        var key = Me();
        Assert.Equal(42, await cache.GetOrCreateAsync(key, _ => new ValueTask<int>(42)));

        // now slightly different func to show delta; should use cached value initially
        await cache.RemoveAsync("unrelated");
        Assert.Equal(42, await cache.GetOrCreateAsync(key, _ => new ValueTask<int>(96)));

        // now remove and repeat - should get updated value
        await cache.RemoveAsync(key);
        Assert.Equal(96, await cache.GetOrCreateAsync(key, _ => new ValueTask<int>(96)));
    }

    [Fact]
    public async Task RemoveNoKeyViaArray()
    {
        using var provider = GetDefaultCache(out var cache);
        var key = Me();
        Assert.Equal(42, await cache.GetOrCreateAsync(key, _ => new ValueTask<int>(42)));

        // now slightly different func to show delta; should use same cached value
        await cache.RemoveAsync([]);
        Assert.Equal(42, await cache.GetOrCreateAsync(key, _ => new ValueTask<int>(96)));
    }

    [Fact]
    public async Task RemoveSingleKeyViaArray()
    {
        using var provider = GetDefaultCache(out var cache);
        var key = Me();
        Assert.Equal(42, await cache.GetOrCreateAsync(key, _ => new ValueTask<int>(42)));

        // now slightly different func to show delta; should use cached value initially
        await cache.RemoveAsync(["unrelated"]);
        Assert.Equal(42, await cache.GetOrCreateAsync(key, _ => new ValueTask<int>(96)));

        // now remove and repeat - should get updated value
        await cache.RemoveAsync([key]);
        Assert.Equal(96, await cache.GetOrCreateAsync(key, _ => new ValueTask<int>(96)));
    }

    [Fact]
    public async Task RemoveMultipleKeysViaArray()
    {
        using var provider = GetDefaultCache(out var cache);
        var key = Me();
        Assert.Equal(42, await cache.GetOrCreateAsync(key, _ => new ValueTask<int>(42)));

        // now slightly different func to show delta; should use cached value initially
        Assert.Equal(42, await cache.GetOrCreateAsync(key, _ => new ValueTask<int>(96)));

        // now remove and repeat - should get updated value
        await cache.RemoveAsync([key, "unrelated"]);
        Assert.Equal(96, await cache.GetOrCreateAsync(key, _ => new ValueTask<int>(96)));
    }

    private static string Me([CallerMemberName] string caller = "") => caller;

}
