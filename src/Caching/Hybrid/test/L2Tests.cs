// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;
public class L2Tests(ITestOutputHelper Log)
{
    class Options<T>(T Value) : IOptions<T> where T : class
    {
        T IOptions<T>.Value => Value;
    }
    ServiceProvider GetDefaultCache(bool buffers, out DefaultHybridCache cache)
    {
        var services = new ServiceCollection();
        var localCacheOptions = new Options<MemoryDistributedCacheOptions>(new());
        var localCache = new MemoryDistributedCache(localCacheOptions);
        services.AddSingleton<IDistributedCache>(buffers ? new BufferLoggingCache(Log, localCache) : new LoggingCache(Log, localCache));
        services.AddHybridCache();
        var provider = services.BuildServiceProvider();
        cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());
        return provider;
    }

    static string CreateString(bool work = false)
    {
        Assert.True(work, "we didn't expect this to be invoked");
        return Guid.NewGuid().ToString();
    }

    static readonly HybridCacheEntryOptions _noL1 = new() { Flags = HybridCacheEntryFlags.DisableLocalCache };

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AssertL2Operations_Immutable(bool buffers)
    {
        using var provider = GetDefaultCache(buffers, out var cache);
        var backend = Assert.IsAssignableFrom<LoggingCache>(cache.BackendCache);
        Log.WriteLine("Inventing key...");
        var s = await cache.GetOrCreateAsync(Me(), ct => new ValueTask<string>(CreateString(true)));
        Assert.Equal(2, backend.OpCount); // GET, SET

        Log.WriteLine("Reading with L1...");
        for (var i = 0; i < 5; i++)
        {
            var x = await cache.GetOrCreateAsync(Me(), ct => new ValueTask<string>(CreateString()));
            Assert.Equal(s, x);
            Assert.Same(s, x);
        }
        Assert.Equal(2, backend.OpCount); // shouldn't be hit

        Log.WriteLine("Reading without L1...");
        for (var i = 0; i < 5; i++)
        {
            var x = await cache.GetOrCreateAsync(Me(), ct => new ValueTask<string>(CreateString()), _noL1);
            Assert.Equal(s, x);
            Assert.NotSame(s, x);
        }
        Assert.Equal(7, backend.OpCount); // should be read every time

        Log.WriteLine("Setting value directly");
        s = CreateString(true);
        await cache.SetAsync(Me(), s);
        for (var i = 0; i < 5; i++)
        {
            var x = await cache.GetOrCreateAsync(Me(), ct => new ValueTask<string>(CreateString()));
            Assert.Equal(s, x);
            Assert.Same(s, x);
        }
        Assert.Equal(8, backend.OpCount); // SET

        Log.WriteLine("Removing key...");
        await cache.RemoveAsync(Me());
        Assert.Equal(9, backend.OpCount); // DEL

        Log.WriteLine("Fetching new...");
        var t = await cache.GetOrCreateAsync(Me(), ct => new ValueTask<string>(CreateString(true)));
        Assert.NotEqual(s, t);
        Assert.Equal(11, backend.OpCount); // GET, SET
    }

    public sealed class Foo
    {
        public string Value { get; set; } = "";
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AssertL2Operations_Mutable(bool buffers)
    {
        using var provider = GetDefaultCache(buffers, out var cache);
        var backend = Assert.IsAssignableFrom<LoggingCache>(cache.BackendCache);
        Log.WriteLine("Inventing key...");
        var s = await cache.GetOrCreateAsync(Me(), ct => new ValueTask<Foo>(new Foo { Value = CreateString(true) }));
        Assert.Equal(2, backend.OpCount); // GET, SET

        Log.WriteLine("Reading with L1...");
        for (var i = 0; i < 5; i++)
        {
            var x = await cache.GetOrCreateAsync(Me(), ct => new ValueTask<Foo>(new Foo { Value = CreateString() }));
            Assert.Equal(s.Value, x.Value);
            Assert.NotSame(s, x);
        }
        Assert.Equal(2, backend.OpCount); // shouldn't be hit

        Log.WriteLine("Reading without L1...");
        for (var i = 0; i < 5; i++)
        {
            var x = await cache.GetOrCreateAsync(Me(), ct => new ValueTask<Foo>(new Foo { Value = CreateString() }), _noL1);
            Assert.Equal(s.Value, x.Value);
            Assert.NotSame(s, x);
        }
        Assert.Equal(7, backend.OpCount); // should be read every time

        Log.WriteLine("Setting value directly");
        s = new Foo { Value = CreateString(true) };
        await cache.SetAsync(Me(), s);
        for (var i = 0; i < 5; i++)
        {
            var x = await cache.GetOrCreateAsync(Me(), ct => new ValueTask<Foo>(new Foo { Value = CreateString() }));
            Assert.Equal(s.Value, x.Value);
            Assert.NotSame(s, x);
        }
        Assert.Equal(8, backend.OpCount); // SET

        Log.WriteLine("Removing key...");
        await cache.RemoveAsync(Me());
        Assert.Equal(9, backend.OpCount); // DEL

        Log.WriteLine("Fetching new...");
        var t = await cache.GetOrCreateAsync(Me(), ct => new ValueTask<Foo>(new Foo { Value = CreateString(true) }));
        Assert.NotEqual(s.Value, t.Value);
        Assert.Equal(11, backend.OpCount); // GET, SET
    }

    class BufferLoggingCache : LoggingCache, IBufferDistributedCache
    {
        public BufferLoggingCache(ITestOutputHelper log, IDistributedCache tail) : base(log, tail) { }

        void IBufferDistributedCache.Set(string key, ReadOnlySequence<byte> value, DistributedCacheEntryOptions options)
        {
            Interlocked.Increment(ref opcount);
            Log.WriteLine($"Set (ROS-byte): {key}");
            Tail.Set(key, value.ToArray(), options);
        }

        ValueTask IBufferDistributedCache.SetAsync(string key, ReadOnlySequence<byte> value, DistributedCacheEntryOptions options, CancellationToken token)
        {
            Interlocked.Increment(ref opcount);
            Log.WriteLine($"SetAsync (ROS-byte): {key}");
            return new(Tail.SetAsync(key, value.ToArray(), options, token));
        }

        bool IBufferDistributedCache.TryGet(string key, IBufferWriter<byte> destination)
        {
            Interlocked.Increment(ref opcount);
            Log.WriteLine($"TryGet: {key}");
            var buffer = Tail.Get(key);
            if (buffer is null)
            {
                return false;
            }
            destination.Write(buffer);
            return true;
        }

        async ValueTask<bool> IBufferDistributedCache.TryGetAsync(string key, IBufferWriter<byte> destination, CancellationToken token)
        {
            Interlocked.Increment(ref opcount);
            Log.WriteLine($"TryGetAsync: {key}");
            var buffer = await Tail.GetAsync(key, token);
            if (buffer is null)
            {
                return false;
            }
            destination.Write(buffer);
            return true;
        }
    }

    class LoggingCache(ITestOutputHelper log, IDistributedCache tail) : IDistributedCache
    {
        protected ITestOutputHelper Log => log;
        protected IDistributedCache Tail => tail;

        protected int opcount;
        public int OpCount => Volatile.Read(ref opcount);

        byte[]? IDistributedCache.Get(string key)
        {
            Interlocked.Increment(ref opcount);
            Log.WriteLine($"Get: {key}");
            return Tail.Get(key);
        }

        Task<byte[]?> IDistributedCache.GetAsync(string key, CancellationToken token)
        {
            Interlocked.Increment(ref opcount);
            Log.WriteLine($"GetAsync: {key}");
            return Tail.GetAsync(key, token);
        }

        void IDistributedCache.Refresh(string key)
        {
            Interlocked.Increment(ref opcount);
            Log.WriteLine($"Refresh: {key}");
            Tail.Refresh(key);
        }

        Task IDistributedCache.RefreshAsync(string key, CancellationToken token)
        {
            Interlocked.Increment(ref opcount);
            Log.WriteLine($"RefreshAsync: {key}");
            return Tail.RefreshAsync(key, token);
        }

        void IDistributedCache.Remove(string key)
        {
            Interlocked.Increment(ref opcount);
            Log.WriteLine($"Remove: {key}");
            Tail.Remove(key);
        }

        Task IDistributedCache.RemoveAsync(string key, CancellationToken token)
        {
            Interlocked.Increment(ref opcount);
            Log.WriteLine($"RemoveAsync: {key}");
            return Tail.RemoveAsync(key, token);
        }

        void IDistributedCache.Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            Interlocked.Increment(ref opcount);
            Log.WriteLine($"Set (byte[]): {key}");
            Tail.Set(key, value, options);
        }

        Task IDistributedCache.SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token)
        {
            Interlocked.Increment(ref opcount);
            Log.WriteLine($"SetAsync (byte[]): {key}");
            return Tail.SetAsync(key, value, options, token);
        }
    }

    private static string Me([CallerMemberName] string caller = "") => caller;
}
