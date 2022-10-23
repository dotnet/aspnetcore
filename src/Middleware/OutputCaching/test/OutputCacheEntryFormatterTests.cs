// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.OutputCaching.Tests;

public class OutputCacheEntryFormatterTests
{
    private static CachedResponseBody EmptyResponseBody = new(new List<byte[]>(), 0);

    [Fact]
    public async Task StoreAndGet_StoresEmptyValues()
    {
        var store = new TestOutputCache();
        var key = "abc";
        var entry = new OutputCacheEntry()
        {
            Body = new CachedResponseBody(new List<byte[]>(), 0),
            Headers = new HeaderDictionary(),
            Tags = Array.Empty<string>()
        };

        await OutputCacheEntryFormatter.StoreAsync(key, entry, TimeSpan.Zero, store, default);

        var result = await OutputCacheEntryFormatter.GetAsync(key, store, default);

        AssertEntriesAreSame(entry, result);
    }

    [Fact]
    public async Task StoreAndGet_StoresAllValues()
    {
        var bodySegment1 = "lorem"u8.ToArray();
        var bodySegment2 = "こんにちは"u8.ToArray();

        var store = new TestOutputCache();
        var key = "abc";
        var entry = new OutputCacheEntry()
        {
            Body = new CachedResponseBody(new List<byte[]>() { bodySegment1, bodySegment2 }, bodySegment1.Length + bodySegment2.Length),
            Created = DateTimeOffset.UtcNow,
            Headers = new HeaderDictionary { [HeaderNames.Accept] = new[] { "text/plain", "text/html" }, [HeaderNames.AcceptCharset] = "utf8" },
            StatusCode = StatusCodes.Status201Created,
            Tags = new[] { "tag", "タグ" }
        };

        await OutputCacheEntryFormatter.StoreAsync(key, entry, TimeSpan.Zero, store, default);

        var result = await OutputCacheEntryFormatter.GetAsync(key, store, default);

        AssertEntriesAreSame(entry, result);
    }

    [Fact]
    public async Task StoreAndGet_StoresNullTags()
    {
        var store = new TestOutputCache();
        var key = "abc";
        var entry = new OutputCacheEntry()
        {
            Body = EmptyResponseBody,
            Headers = new HeaderDictionary(),
            Tags = new[] { null, null, "", "tag" }
        };

        await OutputCacheEntryFormatter.StoreAsync(key, entry, TimeSpan.Zero, store, default);

        var result = await OutputCacheEntryFormatter.GetAsync(key, store, default);

        Assert.Equal(4, result.Tags.Length);
        Assert.Equal("", result.Tags[0]);
        Assert.Equal("", result.Tags[1]);
        Assert.Equal("", result.Tags[2]);
        Assert.Equal("tag", result.Tags[3]);
    }

    [Fact]
    public async Task StoreAndGet_StoresNullHeaders()
    {
        var store = new TestOutputCache();
        var key = "abc";
        var entry = new OutputCacheEntry()
        {
            Body = EmptyResponseBody,
            Headers = new HeaderDictionary { [""] = "", [HeaderNames.Accept] = new[] { null, null, "", "text/html" }, [HeaderNames.AcceptCharset] = new string[] { null } },
            Tags = Array.Empty<string>()
        };

        await OutputCacheEntryFormatter.StoreAsync(key, entry, TimeSpan.Zero, store, default);

        var result = await OutputCacheEntryFormatter.GetAsync(key, store, default);

        Assert.Equal(3, result.Headers.Count);
        Assert.Equal("", result.Headers[""]);
        Assert.Equal(4, result.Headers[HeaderNames.Accept].Count);
        Assert.Equal("", result.Headers[HeaderNames.Accept][0]);
        Assert.Equal("", result.Headers[HeaderNames.Accept][1]);
        Assert.Equal("", result.Headers[HeaderNames.Accept][2]);
        Assert.Equal("text/html", result.Headers[HeaderNames.Accept][3]);
        Assert.Equal("", result.Headers[HeaderNames.AcceptCharset][0]);
    }

    private static void AssertEntriesAreSame(OutputCacheEntry expected, OutputCacheEntry actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);
        Assert.Equal(expected.Tags, actual.Tags);
        Assert.Equal(expected.Created, actual.Created);
        Assert.Equal(expected.StatusCode, actual.StatusCode);
        Assert.Equal(expected.Headers, actual.Headers);
        Assert.Equal(expected.Body.Length, actual.Body.Length);
        Assert.Equal(expected.Body.Segments, actual.Body.Segments);
    }
}
