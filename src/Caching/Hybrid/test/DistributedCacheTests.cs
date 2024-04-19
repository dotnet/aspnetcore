// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;

/// <summary>
/// Validate over-arching expectations of DC implementations, in particular behaviour re IBufferDistributedCache added for HybridCache
/// </summary>
public abstract class DistributedCacheTests
{
    public DistributedCacheTests(ITestOutputHelper log) => Log = log;
    protected ITestOutputHelper Log { get; }
    protected abstract ValueTask ConfigureAsync(IServiceCollection services);
    protected abstract bool CustomClockSupported { get; }

    protected FakeTime Clock { get; } = new();

    protected class FakeTime : TimeProvider, ISystemClock
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;
        public void Reset() => _now = DateTimeOffset.UtcNow;

        DateTimeOffset ISystemClock.UtcNow => _now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Add(TimeSpan delta) => _now += delta;
    }

    private async ValueTask<IServiceCollection> InitAsync()
    {
        Clock.Reset();
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton<ISystemClock>(Clock);
        await ConfigureAsync(services);
        return services;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(128)]
    [InlineData(1024)]
    [InlineData(16 * 1024)]
    public async Task SimpleBufferRoundtrip(int size)
    {
        var cache = (await InitAsync()).BuildServiceProvider().GetService<IDistributedCache>();
        if (cache is null)
        {
            Log.WriteLine("Cache is not available");
            return; // inconclusive
        }

        var key = $"{Me()}:{size}";
        cache.Remove(key);
        Assert.Null(cache.Get(key));

        var expected = new byte[size];
        new Random().NextBytes(expected);
        cache.Set(key, expected, _fiveMinutes);

        var actual = cache.Get(key);
        Assert.NotNull(actual);
        Assert.True(expected.SequenceEqual(actual));
        Log.WriteLine("Data validated");

        if (CustomClockSupported)
        {
            Clock.Add(TimeSpan.FromMinutes(4));
            actual = cache.Get(key);
            Assert.NotNull(actual);
            Assert.True(expected.SequenceEqual(actual));

            Clock.Add(TimeSpan.FromMinutes(2));
            actual = cache.Get(key);
            Assert.Null(actual);

            Log.WriteLine("Expiration validated");
        }
        else
        {
            Log.WriteLine("Expiration not validated - TimeProvider not supported");
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(128)]
    [InlineData(1024)]
    [InlineData(16 * 1024)]
    public async Task SimpleBufferRoundtripAsync(int size)
    {
        var cache = (await InitAsync()).BuildServiceProvider().GetService<IDistributedCache>();
        if (cache is null)
        {
            Log.WriteLine("Cache is not available");
            return; // inconclusive
        }

        var key = $"{Me()}:{size}";
        await cache.RemoveAsync(key);
        Assert.Null(cache.Get(key));

        var expected = new byte[size];
        new Random().NextBytes(expected);
        await cache.SetAsync(key, expected, _fiveMinutes);

        var actual = await cache.GetAsync(key);
        Assert.NotNull(actual);
        Assert.True(expected.SequenceEqual(actual));
        Log.WriteLine("Data validated");

        if (CustomClockSupported)
        {
            Clock.Add(TimeSpan.FromMinutes(4));
            actual = await cache.GetAsync(key);
            Assert.NotNull(actual);
            Assert.True(expected.SequenceEqual(actual));

            Clock.Add(TimeSpan.FromMinutes(2));
            actual = await cache.GetAsync(key);
            Assert.Null(actual);

            Log.WriteLine("Expiration validated");
        }
        else
        {
            Log.WriteLine("Expiration not validated - TimeProvider not supported");
        }
    }

    public enum SequenceKind
    {
        FullArray,
        PaddedArray,
        CustomMemory,
        MultiSegment,
    }

    [Theory]
    [InlineData(0, SequenceKind.FullArray)]
    [InlineData(128, SequenceKind.FullArray)]
    [InlineData(1024, SequenceKind.FullArray)]
    [InlineData(16 * 1024, SequenceKind.FullArray)]
    [InlineData(0, SequenceKind.PaddedArray)]
    [InlineData(128, SequenceKind.PaddedArray)]
    [InlineData(1024, SequenceKind.PaddedArray)]
    [InlineData(16 * 1024, SequenceKind.PaddedArray)]
    [InlineData(0, SequenceKind.CustomMemory)]
    [InlineData(128, SequenceKind.CustomMemory)]
    [InlineData(1024, SequenceKind.CustomMemory)]
    [InlineData(16 * 1024, SequenceKind.CustomMemory)]
    [InlineData(0, SequenceKind.MultiSegment)]
    [InlineData(128, SequenceKind.MultiSegment)]
    [InlineData(1024, SequenceKind.MultiSegment)]
    [InlineData(16 * 1024, SequenceKind.MultiSegment)]
    public async Task ReadOnlySequenceBufferRoundtrip(int size, SequenceKind kind)
    {
        var cache = (await InitAsync()).BuildServiceProvider().GetService<IDistributedCache>() as IBufferDistributedCache;
        if (cache is null)
        {
            Log.WriteLine("Cache is not available or does not support IBufferDistributedCache");
            return; // inconclusive
        }

        var key = $"{Me()}:{size}/{kind}";
        cache.Remove(key);
        Assert.Null(cache.Get(key));

        var payload = Invent(size, kind);
        ReadOnlyMemory<byte> expected = payload.ToArray(); // simplify for testing
        Assert.Equal(size, expected.Length);
        cache.Set(key, payload, _fiveMinutes);

        var writer = RecyclableArrayBufferWriter<byte>.Create(int.MaxValue);
        Assert.True(cache.TryGet(key, writer));
        Assert.True(expected.Span.SequenceEqual(writer.GetCommittedMemory().Span));
        writer.ResetInPlace();
        Log.WriteLine("Data validated");

        if (CustomClockSupported)
        {
            Clock.Add(TimeSpan.FromMinutes(4));
            Assert.True(cache.TryGet(key, writer));
            Assert.True(expected.Span.SequenceEqual(writer.GetCommittedMemory().Span));
            writer.ResetInPlace();

            Clock.Add(TimeSpan.FromMinutes(2));
            Assert.False(cache.TryGet(key, writer));
            Assert.Equal(0, writer.CommittedBytes);

            Log.WriteLine("Expiration validated");
        }
        else
        {
            Log.WriteLine("Expiration not validated - TimeProvider not supported");
        }
    }

    [Theory]
    [InlineData(0, SequenceKind.FullArray)]
    [InlineData(128, SequenceKind.FullArray)]
    [InlineData(1024, SequenceKind.FullArray)]
    [InlineData(16 * 1024, SequenceKind.FullArray)]
    [InlineData(0, SequenceKind.PaddedArray)]
    [InlineData(128, SequenceKind.PaddedArray)]
    [InlineData(1024, SequenceKind.PaddedArray)]
    [InlineData(16 * 1024, SequenceKind.PaddedArray)]
    [InlineData(0, SequenceKind.CustomMemory)]
    [InlineData(128, SequenceKind.CustomMemory)]
    [InlineData(1024, SequenceKind.CustomMemory)]
    [InlineData(16 * 1024, SequenceKind.CustomMemory)]
    [InlineData(0, SequenceKind.MultiSegment)]
    [InlineData(128, SequenceKind.MultiSegment)]
    [InlineData(1024, SequenceKind.MultiSegment)]
    [InlineData(16 * 1024, SequenceKind.MultiSegment)]
    public async Task ReadOnlySequenceBufferRoundtripAsync(int size, SequenceKind kind)
    {
        var cache = (await InitAsync()).BuildServiceProvider().GetService<IDistributedCache>() as IBufferDistributedCache;
        if (cache is null)
        {
            Log.WriteLine("Cache is not available or does not support IBufferDistributedCache");
            return; // inconclusive
        }

        var key = $"{Me()}:{size}/{kind}";
        await cache.RemoveAsync(key);
        Assert.Null(await cache.GetAsync(key));

        var payload = Invent(size, kind);
        ReadOnlyMemory<byte> expected = payload.ToArray(); // simplify for testing
        Assert.Equal(size, expected.Length);
        await cache.SetAsync(key, payload, _fiveMinutes);

        var writer = RecyclableArrayBufferWriter<byte>.Create(int.MaxValue);
        Assert.True(await cache.TryGetAsync(key, writer));
        Assert.True(expected.Span.SequenceEqual(writer.GetCommittedMemory().Span));
        writer.ResetInPlace();
        Log.WriteLine("Data validated");

        if (CustomClockSupported)
        {
            Clock.Add(TimeSpan.FromMinutes(4));
            Assert.True(await cache.TryGetAsync(key, writer));
            Assert.True(expected.Span.SequenceEqual(writer.GetCommittedMemory().Span));
            writer.ResetInPlace();

            Clock.Add(TimeSpan.FromMinutes(2));
            Assert.False(await cache.TryGetAsync(key, writer));
            Assert.Equal(0, writer.CommittedBytes);

            Log.WriteLine("Expiration validated");
        }
        else
        {
            Log.WriteLine("Expiration not validated - TimeProvider not supported");
        }
    }

    static ReadOnlySequence<byte> Invent(int size, SequenceKind kind)
    {
        var rand = new Random();
        ReadOnlySequence<byte> payload;
        switch (kind)
        {
            case SequenceKind.FullArray:
                var arr = new byte[size];
                rand.NextBytes(arr);
                payload = new(arr);
                break;
            case SequenceKind.PaddedArray:
                arr = new byte[size + 10];
                rand.NextBytes(arr);
                payload = new(arr, 5, arr.Length - 10);
                break;
            case SequenceKind.CustomMemory:
                var mem = new CustomMemory(size, rand).Memory;
                payload = new(mem);
                break;
            case SequenceKind.MultiSegment:
                if (size == 0)
                {
                    payload = default;
                    break;
                }
                if (size < 10)
                {
                    throw new ArgumentException("small segments not considered"); // a pain to construct
                }
                CustomSegment first = new(10, rand, null), // we'll take the last 3 of this 10
                    second = new(size - 7, rand, first), // we'll take all of this one
                    third = new(10, rand, second); // we'll take the first 4 of this 10
                payload = new(first, 7, third, 4);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }

        // now validate what we expect of that payload
        Assert.Equal(size, payload.Length);
        switch (kind)
        {
            case SequenceKind.CustomMemory or SequenceKind.MultiSegment when size == 0:
                Assert.True(payload.IsSingleSegment);
                Assert.True(MemoryMarshal.TryGetArray(payload.First, out _));
                break;
            case SequenceKind.MultiSegment:
                Assert.False(payload.IsSingleSegment);
                break;
            case SequenceKind.CustomMemory:
                Assert.True(payload.IsSingleSegment);
                Assert.False(MemoryMarshal.TryGetArray(payload.First, out _));
                break;
            case SequenceKind.FullArray:
                Assert.True(payload.IsSingleSegment);
                Assert.True(MemoryMarshal.TryGetArray(payload.First, out var segment));
                Assert.Equal(0, segment.Offset);
                Assert.NotNull(segment.Array);
                Assert.Equal(size, segment.Count);
                Assert.Equal(size, segment.Array.Length);
                break;
            case SequenceKind.PaddedArray:
                Assert.True(payload.IsSingleSegment);
                Assert.True(MemoryMarshal.TryGetArray(payload.First, out segment));
                Assert.NotEqual(0, segment.Offset);
                Assert.NotNull(segment.Array);
                Assert.Equal(size, segment.Count);
                Assert.NotEqual(size, segment.Array.Length);
                break;
        }
        return payload;
    }

    class CustomSegment : ReadOnlySequenceSegment<byte>
    {
        public CustomSegment(int size, Random? rand, CustomSegment? previous)
        {
            var arr = new byte[size + 10];
            rand?.NextBytes(arr);
            Memory = new(arr, 5, arr.Length - 10);
            if (previous is not null)
            {
                RunningIndex = previous.RunningIndex + previous.Memory.Length;
                previous.Next = this;
            }
        }
    }

    class CustomMemory : MemoryManager<byte>
    {
        private readonly byte[] _data;
        public CustomMemory(int size, Random? rand = null)
        {
            _data = new byte[size + 10];
            rand?.NextBytes(_data);
        }
        public override Span<byte> GetSpan() => new(_data, 5, _data.Length - 10);
        public override MemoryHandle Pin(int elementIndex = 0) => throw new NotSupportedException();
        public override void Unpin() => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { }
        protected override bool TryGetArray(out ArraySegment<byte> segment)
        {
            segment = default;
            return false;
        }
    }

    private static readonly DistributedCacheEntryOptions _fiveMinutes
        = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };

    protected static string Me([CallerMemberName] string caller = "") => caller;
}
