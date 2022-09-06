// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.OutputCaching.Tests;

public class OutputCacheEntryFormatterTests
{
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
        var store = new TestOutputCache();
        var key = "abc";
        var entry = new OutputCacheEntry()
        {
            Body = new CachedResponseBody(new List<byte[]>() { "lorem"u8.ToArray(), "ipsum"u8.ToArray() }, 10),
            Created = DateTimeOffset.UtcNow,
            Headers = new HeaderDictionary { [HeaderNames.Accept] = "text/plain", [HeaderNames.AcceptCharset] = "utf8" },
            StatusCode = StatusCodes.Status201Created,
            Tags = new[] { "tag1", "tag2" }
        };

        await OutputCacheEntryFormatter.StoreAsync(key, entry, TimeSpan.Zero, store, default);

        var result = await OutputCacheEntryFormatter.GetAsync(key, store, default);

        AssertEntriesAreSame(entry, result);
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
