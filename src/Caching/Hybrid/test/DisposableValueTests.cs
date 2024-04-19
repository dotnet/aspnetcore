// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Hybrid.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;
public class DisposableValueTests
{
    // We can only reasonable be expected to be responsible for disposal (IDisposable) of objects
    // if we're keeping hold of references, which means: things we consider immutable.
    // It is noted that this creates an oddity whereby for *mutable* types, the caller needs to dispose
    // per fetch (GetOrCreateAsync), and for *immutable* types: they're not - but that is unavoidable.
    // In reality, it is expected to be pretty rare to hold disposable types here.

    private static ServiceProvider GetCache(out DefaultHybridCache cache)
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        var provider = services.BuildServiceProvider();
        cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());
        return provider;
    }

    [Fact]
    public async void NonDisposableImmutableTypeDoesNotNeedEvictionCallback()
    {
        using var provider = GetCache(out var cache);
        var key = Me();

        var s = await cache.GetOrCreateAsync(key, _ => GetSomeString());
        Assert.NotNull(s);
        Assert.False(string.IsNullOrWhiteSpace(s));
        Assert.True(cache.DebugTryGetCacheItem(key, out var cacheItem));
        Assert.True(cacheItem.DebugIsImmutable);
        Assert.False(cacheItem.NeedsEvictionCallback);

        static ValueTask<string> GetSomeString() => new(Guid.NewGuid().ToString());
    }

    [Fact]
    public async void NonDisposableBlittableTypeDoesNotNeedEvictionCallback()
    {
        using var provider = GetCache(out var cache);
        var key = Me();

        var g = await cache.GetOrCreateAsync(key, _ => GetSomeGuid());
        Assert.NotEqual(Guid.Empty, g);
        Assert.True(cache.DebugTryGetCacheItem(key, out var cacheItem));
        Assert.True(cacheItem.DebugIsImmutable);
        Assert.False(cacheItem.NeedsEvictionCallback);

        static ValueTask<Guid> GetSomeGuid() => new(Guid.NewGuid());
    }

    [Fact]
    public async Task DispsableRefTypeNeedsEvictionCallback()
    {
        using var provider = GetCache(out var cache);
        var key = Me();

        var inst = new DisposableTestClass();
        var obj = await cache.GetOrCreateAsync(key, _ => new ValueTask<DisposableTestClass>(inst));
        Assert.Same(inst, obj);
        Assert.True(cache.DebugTryGetCacheItem(key, out var cacheItem));
        Assert.True(cacheItem.DebugIsImmutable);
        Assert.True(cacheItem.NeedsEvictionCallback);

        Assert.Equal(0, inst.DisposeCount);

        // now remove it
        await cache.RemoveKeyAsync(key);

        // give it a moment for the eviction callback to kick in
        for (var i = 0; i < 10; i++)
        {
            await Task.Delay(250);
            if (inst.DisposeCount != 0)
            {
                break;
            }
        }
        Assert.Equal(1, inst.DisposeCount);
    }

    [Fact]
    public async Task DisposableValueTypeNeedsEvictionCallback()
    {
        using var provider = GetCache(out var cache);
        var key = Me();

        // disposal of value-type
        var inst = new DisposableTestClass();
        var v = await cache.GetOrCreateAsync(key, _ => new ValueTask<DisposableTestValue>(new DisposableTestValue(inst)));
        Assert.Same(inst, v.Wrapped);
        Assert.True(cache.DebugTryGetCacheItem(key, out var cacheItem));
        Assert.True(cacheItem.DebugIsImmutable);
        Assert.True(cacheItem.NeedsEvictionCallback);

        Assert.Equal(0, inst.DisposeCount);

        // now remove it
        await cache.RemoveKeyAsync(key);

        // give it a moment for the eviction callback to kick in
        for (var i = 0; i < 10; i++)
        {
            await Task.Delay(250);
            if (inst.DisposeCount != 0)
            {
                break;
            }
        }
        Assert.Equal(1, inst.DisposeCount);
    }

    [Fact]
    public async Task NonDispsableRefTypeDoesNotNeedEvictionCallback()
    {
        using var provider = GetCache(out var cache);
        var key = Me();

        var inst = new NonDisposableTestClass();
        var obj = await cache.GetOrCreateAsync(key, _ => new ValueTask<NonDisposableTestClass>(inst));
        Assert.Same(inst, obj);
        Assert.True(cache.DebugTryGetCacheItem(key, out var cacheItem));
        Assert.True(cacheItem.DebugIsImmutable);
        Assert.False(cacheItem.NeedsEvictionCallback);
    }

    [Fact]
    public async Task NonDisposableValueTypeDoesNotNeedEvictionCallback()
    {
        using var provider = GetCache(out var cache);
        var key = Me();

        // disposal of value-type
        var inst = new DisposableTestClass();
        var v = await cache.GetOrCreateAsync(key, _ => new ValueTask<NonDisposableTestValue>(new NonDisposableTestValue(inst)));
        Assert.Same(inst, v.Wrapped);
        Assert.True(cache.DebugTryGetCacheItem(key, out var cacheItem));
        Assert.True(cacheItem.DebugIsImmutable);
        Assert.False(cacheItem.NeedsEvictionCallback);
    }

    [ImmutableObject(true)]
    public sealed class DisposableTestClass : IDisposable
    {
        private int _disposeCount;
        public void Dispose() => Interlocked.Increment(ref _disposeCount);

        public int DisposeCount => Volatile.Read(ref _disposeCount);
    }

    [ImmutableObject(true)]
    public readonly struct DisposableTestValue : IDisposable
    {
        public DisposableTestClass Wrapped { get; }
        public DisposableTestValue(DisposableTestClass inner) => Wrapped = inner;
        public void Dispose() => Wrapped.Dispose();
    }

    [ImmutableObject(true)]
    public sealed class NonDisposableTestClass
    {
        private int _disposeCount;
        public void Dispose() => Interlocked.Increment(ref _disposeCount);

        public int DisposeCount => Volatile.Read(ref _disposeCount);
    }

    [ImmutableObject(true)]
    public readonly struct NonDisposableTestValue
    {
        public DisposableTestClass Wrapped { get; }
        public NonDisposableTestValue(DisposableTestClass inner) => Wrapped = inner;
        public void Dispose() => Wrapped.Dispose();
    }

    private static string Me([CallerMemberName] string caller = "") => caller;
}
