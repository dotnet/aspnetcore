// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.OutputCaching.StackExchangeRedis.Tests;

public class OutputCacheGetSetTests : IClassFixture<RedisConnectionFixture>
{
    private const string SkipReason = "TODO: Disabled due to CI failure. " +
    "These tests require Redis server to be started on the machine. Make sure to change the value of" +
    "\"RedisTestConfig.RedisPort\" accordingly.";

    private readonly IOutputCacheBufferStore _cache;
    private readonly RedisConnectionFixture _fixture;
    private readonly ITestOutputHelper Log;

    public OutputCacheGetSetTests(RedisConnectionFixture connection, ITestOutputHelper log)
    {
        // use DI to get the configured service, but tweak the GC mode
        _fixture = connection;
        Log = log;
        var services = new ServiceCollection();
        services.AddStackExchangeRedisOutputCache(options => {
            options.ConnectionMultiplexerFactory = () => Task.FromResult(_fixture.Connection);
            options.InstanceName = "TestPrefix";
        });
        var svc = Assert.IsAssignableFrom<RedisOutputCacheStore>(
            services.BuildServiceProvider().GetService<IOutputCacheStore>());
        Assert.NotNull(svc);
        svc.GarbageCollectionEnabled = false;
        _cache = svc;
    }

#if DEBUG
    private async ValueTask<IOutputCacheBufferStore> Cache()
    {
        if (_cache is RedisOutputCacheStore real)
        {
            Log.WriteLine(await real.GetConfigurationInfoAsync().ConfigureAwait(false));
        }
        return _cache;
    }
#else
    private ValueTask<IOutputCacheBufferStore> Cache() => new(_cache); // avoid CS1998 - no "await"
#endif

    [Fact(Skip = SkipReason)]
    public async Task GetMissingKeyReturnsNull()
    {
        var cache = await Cache().ConfigureAwait(false);
        var result = await cache.GetAsync("non-existent-key", CancellationToken.None);
        Assert.Null(result);
    }

    [Theory(Skip = SkipReason)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task SetStoresValueWithPrefixAndTimeout(bool useReadOnlySequence, bool withTags)
    {
        var cache = await Cache().ConfigureAwait(false);
        var key = Guid.NewGuid().ToString();
        byte[] storedValue = new byte[1017];
        Random.Shared.NextBytes(storedValue);
        RedisKey underlyingKey = "TestPrefix__MSOCV_" + key;

        // pre-check
        await _fixture.Database.KeyDeleteAsync(new RedisKey[] { "TestPrefix__MSOCT", "TestPrefix__MSOCT_tagA", "TestPrefix__MSOCT_tagB" });
        var timeout = await _fixture.Database.KeyTimeToLiveAsync(underlyingKey);
        Assert.Null(timeout); // means doesn't exist
        Assert.False(await _fixture.Database.KeyExistsAsync("TestPrefix__MSOCT"));
        Assert.False(await _fixture.Database.KeyExistsAsync("TestPrefix__MSOCT_tagA"));
        Assert.False(await _fixture.Database.KeyExistsAsync("TestPrefix__MSOCT_tagB"));

        // act
        var actTime = DateTime.UtcNow;
        var ttl = TimeSpan.FromSeconds(30);
        var tags = withTags ? new[] { "tagA", "tagB" } : null;
        if (useReadOnlySequence)
        {
            await cache.SetAsync(key, new ReadOnlySequence<byte>(storedValue), tags, ttl, CancellationToken.None);
        }
        else
        {
            await cache.SetAsync(key, storedValue, tags, ttl, CancellationToken.None);
        }

        // validate via redis direct
        timeout = await _fixture.Database.KeyTimeToLiveAsync(underlyingKey);
        Assert.NotNull(timeout); // means exists
        var seconds = timeout.Value.TotalSeconds;
        Assert.True(seconds >= 28 && seconds <= 32, "timeout should be in range");
        var redisValue = (byte[])(await _fixture.Database.StringGetAsync(underlyingKey));
        Assert.True(((ReadOnlySpan<byte>)storedValue).SequenceEqual(redisValue), "payload should match");

        double expected = (long)((actTime + ttl) - DateTime.UnixEpoch).TotalMilliseconds;
        if (withTags)
        {
            // we expect the tag structure to now exist, with the scores within a bit of a second
            const double Tolerance = 100.0;
            Assert.Equal((await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT", "tagA")).Value, expected, Tolerance);
            Assert.Equal((await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT", "tagB")).Value, expected, Tolerance);
            Assert.Equal((await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_tagA", key)).Value, expected, Tolerance);
            Assert.Equal((await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_tagB", key)).Value, expected, Tolerance);
        }
        else
        {
            // we do *not* expect the tag structure to exist
            Assert.False(await _fixture.Database.KeyExistsAsync("TestPrefix__MSOCT"));
            Assert.False(await _fixture.Database.KeyExistsAsync("TestPrefix__MSOCT_tagA"));
            Assert.False(await _fixture.Database.KeyExistsAsync("TestPrefix__MSOCT_tagB"));
        }
    }

    [Theory(Skip = SkipReason)]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CanFetchStoredValue(bool useReadOnlySequence)
    {
        var cache = await Cache().ConfigureAwait(false);
        var key = Guid.NewGuid().ToString();
        byte[] storedValue = new byte[1017];
        Random.Shared.NextBytes(storedValue);

        // pre-check
        var fetchedValue = await cache.GetAsync(key, CancellationToken.None);
        Assert.Null(fetchedValue);

        // store and fetch via service
        if (useReadOnlySequence)
        {
            await cache.SetAsync(key, new ReadOnlySequence<byte>(storedValue), null, TimeSpan.FromSeconds(30), CancellationToken.None);
        }
        else
        {
            await cache.SetAsync(key, storedValue, null, TimeSpan.FromSeconds(30), CancellationToken.None);
        }
        fetchedValue = await cache.GetAsync(key, CancellationToken.None);
        Assert.NotNull(fetchedValue);

        Assert.True(((ReadOnlySpan<byte>)storedValue).SequenceEqual(fetchedValue), "payload should match");
    }

    [Fact(Skip = SkipReason)]
    public async Task GetMissingKeyReturnsFalse_BufferWriter() // "true" result checked in MultiSegmentWriteWorksAsExpected_BufferWriter
    {
        var cache = Assert.IsAssignableFrom<IOutputCacheBufferStore>(await Cache().ConfigureAwait(false));
        var key = Guid.NewGuid().ToString();

        var pipe = new Pipe();
        Assert.False(await cache.TryGetAsync(key, pipe.Writer, CancellationToken.None));
        pipe.Writer.Complete();
        var read = await pipe.Reader.ReadAsync();
        Assert.True(read.IsCompleted);
        Assert.True(read.Buffer.IsEmpty);
        pipe.Reader.AdvanceTo(read.Buffer.End);
    }

    [Fact(Skip = SkipReason)]
    public async Task MasterTagScoreShouldOnlyIncrease()
    {
        // store some data
        var cache = await Cache().ConfigureAwait(false);
        byte[] storedValue = new byte[1017];
        Random.Shared.NextBytes(storedValue);
        var tags = new[] { "gtonly" };
        await _fixture.Database.KeyDeleteAsync("TestPrefix__MSOCT"); // start from nil state

        await cache.SetAsync(Guid.NewGuid().ToString(), storedValue, tags, TimeSpan.FromSeconds(30), CancellationToken.None);
        var originalScore = await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT", "gtonly");
        Assert.NotNull(originalScore);

        // now store something with a shorter ttl; the score should not change
        await cache.SetAsync(Guid.NewGuid().ToString(), storedValue, tags, TimeSpan.FromSeconds(15), CancellationToken.None);
        var newScore = await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT", "gtonly");
        Assert.NotNull(newScore);
        Assert.Equal(originalScore, newScore);

        // now store something with a longer ttl; the score should increase
        await cache.SetAsync(Guid.NewGuid().ToString(), storedValue, tags, TimeSpan.FromSeconds(45), CancellationToken.None);
        newScore = await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT", "gtonly");
        Assert.NotNull(newScore);
        Assert.True(newScore > originalScore, "should increase");
    }

    [Fact(Skip = SkipReason)]
    public async Task CanEvictByTag()
    {
        // store some data
        var cache = await Cache().ConfigureAwait(false);
        byte[] storedValue = new byte[1017];
        Random.Shared.NextBytes(storedValue);
        var ttl = TimeSpan.FromSeconds(30);

        var noTags = Guid.NewGuid().ToString();
        await cache.SetAsync(noTags, storedValue, null, ttl, CancellationToken.None);

        var foo = Guid.NewGuid().ToString();
        await cache.SetAsync(foo, storedValue, new[] { "foo" }, ttl, CancellationToken.None);

        var bar = Guid.NewGuid().ToString();
        await cache.SetAsync(bar, storedValue, new[] { "bar" }, ttl, CancellationToken.None);

        var fooBar = Guid.NewGuid().ToString();
        await cache.SetAsync(fooBar, storedValue, new[] { "foo", "bar" }, ttl, CancellationToken.None);

        // assert prior state
        Assert.NotNull(await cache.GetAsync(noTags, CancellationToken.None));
        Assert.NotNull(await cache.GetAsync(foo, CancellationToken.None));
        Assert.NotNull(await cache.GetAsync(bar, CancellationToken.None));
        Assert.NotNull(await cache.GetAsync(fooBar, CancellationToken.None));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_foo", noTags));
        Assert.NotNull(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_foo", foo));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_foo", bar));
        Assert.NotNull(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_foo", fooBar));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_bar", noTags));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_bar", foo));
        Assert.NotNull(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_bar", bar));
        Assert.NotNull(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_bar", fooBar));

        // act
        for (int i = 0; i < 2; i++) // loop is to ensure no oddity when working on tags that *don't* have entries
        {
            await cache.EvictByTagAsync("foo", CancellationToken.None);
            Assert.NotNull(await cache.GetAsync(noTags, CancellationToken.None));
            Assert.Null(await cache.GetAsync(foo, CancellationToken.None));
            Assert.NotNull(await cache.GetAsync(bar, CancellationToken.None));
            Assert.Null(await cache.GetAsync(fooBar, CancellationToken.None));
        }
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_foo", noTags));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_foo", foo));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_foo", bar));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_foo", fooBar));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_bar", noTags));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_bar", foo));
        Assert.NotNull(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_bar", bar));
        Assert.NotNull(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_bar", fooBar));

        for (int i = 0; i < 2; i++) // loop is to ensure no oddity when working on tags that *don't* have entries
        {
            await cache.EvictByTagAsync("bar", CancellationToken.None);
            Assert.NotNull(await cache.GetAsync(noTags, CancellationToken.None));
            Assert.Null(await cache.GetAsync(foo, CancellationToken.None));
            Assert.Null(await cache.GetAsync(bar, CancellationToken.None));
            Assert.Null(await cache.GetAsync(fooBar, CancellationToken.None));
        }
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_foo", noTags));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_foo", foo));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_foo", bar));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_foo", fooBar));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_bar", noTags));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_bar", foo));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_bar", bar));
        Assert.Null(await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT_bar", fooBar));

        // assert expected state
        Assert.NotNull(await cache.GetAsync(noTags, CancellationToken.None));
        Assert.Null(await cache.GetAsync(foo, CancellationToken.None));
        Assert.Null(await cache.GetAsync(bar, CancellationToken.None));
        Assert.Null(await cache.GetAsync(fooBar, CancellationToken.None));
    }

    [Fact(Skip = SkipReason)]
    public async Task MultiSegmentWriteWorksAsExpected_Array()
    {
        // store some data
        var first = new Segment(1024, null);
        var second = new Segment(1024, first);
        var third = new Segment(1024, second);

        Random.Shared.NextBytes(first.Array);
        Random.Shared.NextBytes(second.Array);
        Random.Shared.NextBytes(third.Array);
        var payload = new ReadOnlySequence<byte>(first, 800, third, 42);
        Assert.False(payload.IsSingleSegment, "multi-segment");
        Assert.Equal(1290, payload.Length); // partial from first and last

        var cache = await Cache().ConfigureAwait(false);
        var key = Guid.NewGuid().ToString();
        await cache.SetAsync(key, payload, default, TimeSpan.FromSeconds(30), CancellationToken.None);

        var fetched = await cache.GetAsync(key, CancellationToken.None);
        Assert.NotNull(fetched);
        ReadOnlyMemory<byte> linear = payload.ToArray();
        Assert.True(linear.Span.SequenceEqual(fetched), "payload match");
    }

    [Fact(Skip = SkipReason)]
    public async Task MultiSegmentWriteWorksAsExpected_BufferWriter()
    {
        // store some data
        var first = new Segment(1024, null);
        var second = new Segment(1024, first);
        var third = new Segment(1024, second);

        Random.Shared.NextBytes(first.Array);
        Random.Shared.NextBytes(second.Array);
        Random.Shared.NextBytes(third.Array);
        var payload = new ReadOnlySequence<byte>(first, 800, third, 42);
        Assert.False(payload.IsSingleSegment, "multi-segment");
        Assert.Equal(1290, payload.Length); // partial from first and last

        var cache = Assert.IsAssignableFrom<IOutputCacheBufferStore>(await Cache().ConfigureAwait(false));
        var key = Guid.NewGuid().ToString();
        await cache.SetAsync(key, payload, default, TimeSpan.FromSeconds(30), CancellationToken.None);

        var pipe = new Pipe();
        Assert.True(await cache.TryGetAsync(key, pipe.Writer, CancellationToken.None));
        pipe.Writer.Complete();
        var read = await pipe.Reader.ReadAsync();
        Assert.True(read.IsCompleted);
        Assert.Equal(1290, read.Buffer.Length);

        using (Linearize(payload, out var linearPayload))
        using (Linearize(read.Buffer, out var linearRead))
        {
            Assert.True(linearPayload.Span.SequenceEqual(linearRead.Span), "payload match");
        }
        pipe.Reader.AdvanceTo(read.Buffer.End);

        static IMemoryOwner<byte> Linearize(ReadOnlySequence<byte> payload, out ReadOnlyMemory<byte> linear)
        {
            if (payload.IsEmpty)
            {
                linear = default;
                return null;
            }
            if (payload.IsSingleSegment)
            {
                linear = payload.First;
                return null;
            }
            var len = checked((int)payload.Length);
            var lease = MemoryPool<byte>.Shared.Rent(len);
            var memory = lease.Memory.Slice(0, len);
            payload.CopyTo(memory.Span);
            linear = memory;
            return lease;
        }
    }

    [Fact(Skip = SkipReason)]
    public async Task GarbageCollectionDoesNotRunWhenGCKeyHeld()
    {
        var cache = await Cache().ConfigureAwait(false);
        var impl = Assert.IsAssignableFrom<RedisOutputCacheStore>(cache);
        await _fixture.Database.StringSetAsync("TestPrefix__MSOCTGC", "dummy", TimeSpan.FromMinutes(1));
        try
        {
            Assert.Null(await impl.ExecuteGarbageCollectionAsync(42));
        }
        finally
        {
            await _fixture.Database.KeyDeleteAsync("TestPrefix__MSOCTGC");
        }
    }

    [Fact(Skip = SkipReason)]
    public async Task GarbageCollectionCleansUpTagData()
    {
        // importantly, we're not interested in the lifetime of the *values* - redis deals with that
        // itself; we're only interested in the tag-expiry metadata
        var blob = new byte[16];
        Random.Shared.NextBytes(blob);
        var cache = await Cache().ConfigureAwait(false);
        var impl = Assert.IsAssignableFrom<RedisOutputCacheStore>(cache);

        // start vanilla
        await _fixture.Database.KeyDeleteAsync(new RedisKey[] { "TestPrefix__MSOCT",
            "TestPrefix__MSOCT_a", "TestPrefix__MSOCT_b",
            "TestPrefix__MSOCT_c", "TestPrefix__MSOCT_d" });

        await cache.SetAsync(Guid.NewGuid().ToString(), blob, new[] { "a", "b" }, TimeSpan.FromSeconds(5), CancellationToken.None); // a=b=5
        await cache.SetAsync(Guid.NewGuid().ToString(), blob, new[] { "b", "c" }, TimeSpan.FromSeconds(10), CancellationToken.None); // a=5, b=c=10
        await cache.SetAsync(Guid.NewGuid().ToString(), blob, new[] { "c", "d" }, TimeSpan.FromSeconds(15), CancellationToken.None); // a=5, b=10, c=d=15

        long aScore = (long)await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT", "a"),
             bScore = (long)await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT", "b"),
             cScore = (long)await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT", "c"),
             dScore = (long)await _fixture.Database.SortedSetScoreAsync("TestPrefix__MSOCT", "d");

        Assert.False(await _fixture.Database.KeyExistsAsync("TestPrefix__MSOCTGC"), "GC key should not exist");
        Assert.True(await _fixture.Database.KeyExistsAsync("TestPrefix__MSOCT"), "master tag should exist");
        Assert.True(await _fixture.Database.KeyExistsAsync("TestPrefix__MSOCT_a"), "tag a should exist");
        Assert.True(await _fixture.Database.KeyExistsAsync("TestPrefix__MSOCT_b"), "tag b should exist");
        Assert.True(await _fixture.Database.KeyExistsAsync("TestPrefix__MSOCT_c"), "tag c should exist");
        Assert.True(await _fixture.Database.KeyExistsAsync("TestPrefix__MSOCT_d"), "tag d should exist");

        await CheckCounts(4, 1, 2, 2, 1);
        Assert.Equal(0, await impl.ExecuteGarbageCollectionAsync(0)); // should not change anything
        await CheckCounts(4, 1, 2, 2, 1);
        Assert.Equal(1, await impl.ExecuteGarbageCollectionAsync(aScore)); // 1=removes a
        await CheckCounts(3, 0, 1, 2, 1);
        Assert.Equal(1, await impl.ExecuteGarbageCollectionAsync(bScore)); // 1=removes b
        await CheckCounts(2, 0, 0, 1, 1);
        Assert.Equal(2, await impl.ExecuteGarbageCollectionAsync(cScore)); // 2=removes c+d
        await CheckCounts(0, 0, 0, 0, 0);
        Assert.Equal(0, await impl.ExecuteGarbageCollectionAsync(dScore));
        await CheckCounts(0, 0, 0, 0, 0);
        Assert.Equal(0, await impl.ExecuteGarbageCollectionAsync(dScore + 1000)); // should have nothing left to do
        await CheckCounts(0, 0, 0, 0, 0);

        // we should now not have any of these left
        Assert.False(await _fixture.Database.KeyExistsAsync("TestPrefix__MSOCTGC"), "GC key should not exist");
        Assert.False(await _fixture.Database.KeyExistsAsync("TestPrefix__MSOCT"), "master tag still exists");
        Assert.False(await _fixture.Database.KeyExistsAsync("TestPrefix__MSOCT_a"), "tag a still exists");
        Assert.False(await _fixture.Database.KeyExistsAsync("TestPrefix__MSOCT_b"), "tag b still exists");
        Assert.False(await _fixture.Database.KeyExistsAsync("TestPrefix__MSOCT_c"), "tag c still exists");
        Assert.False(await _fixture.Database.KeyExistsAsync("TestPrefix__MSOCT_d"), "tag d still exists");

        async Task CheckCounts(int master, int a, int b, int c, int d)
        {
            Assert.Equal(master, (int)await _fixture.Database.SortedSetLengthAsync("TestPrefix__MSOCT"));
            Assert.Equal(a, (int)await _fixture.Database.SortedSetLengthAsync("TestPrefix__MSOCT_a"));
            Assert.Equal(b, (int)await _fixture.Database.SortedSetLengthAsync("TestPrefix__MSOCT_b"));
            Assert.Equal(c, (int)await _fixture.Database.SortedSetLengthAsync("TestPrefix__MSOCT_c"));
            Assert.Equal(d, (int)await _fixture.Database.SortedSetLengthAsync("TestPrefix__MSOCT_d"));
        }
    }

    private class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(int length, Segment previous = null)
        {
            if (previous is not null)
            {
                previous.Next = this;
                RunningIndex = previous.RunningIndex + previous.Memory.Length;
            }
            Array = new byte[length];
            Memory = Array;
        }
        public byte[] Array { get; }
    }
}
