// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.OutputCaching.Tests;

public class OutputCacheEntryFormatterTests_SimpleStore : OutputCacheEntryFormatterTests
{
    public override ITestOutputCacheStore GetStore() => new SimpleTestOutputCache();
}

public class OutputCacheEntryFormatterTests_BufferStore : OutputCacheEntryFormatterTests
{
    public override ITestOutputCacheStore GetStore() => new BufferTestOutputCache();
}

public abstract class OutputCacheEntryFormatterTests
{
    public abstract ITestOutputCacheStore GetStore();

    // arbitrarily some time 17 May 2023 - so we can predict payloads
    static readonly DateTimeOffset KnownTime = DateTimeOffset.FromUnixTimeMilliseconds(1684322693875);

    [Fact]
    public async Task StoreAndGet_StoresEmptyValues()
    {
        var store = GetStore();
        var key = "abc";
        using var entry = new OutputCacheEntry(KnownTime, StatusCodes.Status200OK);

        await OutputCacheEntryFormatter.StoreAsync(key, entry, null, TimeSpan.Zero, store, NullLogger.Instance, default);

        var result = await OutputCacheEntryFormatter.GetAsync(key, store, default);

        AssertEntriesAreSame(entry, result);
    }

    [Fact]
    public async Task StoreAndGet_StoresAllValues()
    {
        var bodySegment1 = "lorem"u8.ToArray();
        var bodySegment2 = "こんにちは"u8.ToArray();

        var store = GetStore();
        var key = "abc";
        using (var entry = new OutputCacheEntry(KnownTime, StatusCodes.Status201Created)
            .CopyHeadersFrom(new HeaderDictionary { [HeaderNames.Accept] = new[] { "text/plain", "text/html" }, [HeaderNames.AcceptCharset] = "utf8" })
            .CreateBodyFrom(new[] { bodySegment1, bodySegment1 }))
        {
            await OutputCacheEntryFormatter.StoreAsync(key, entry, new HashSet<string>() { "tag", "タグ" }, TimeSpan.Zero, store, NullLogger.Instance, default);
            var result = await OutputCacheEntryFormatter.GetAsync(key, store, default);

            AssertEntriesAreSame(entry, result);
        }
    }

    [Fact]

    public async Task StoreAndGet_StoresNullHeaders()
    {
        var store = GetStore();
        var key = "abc";

        using (var entry = new OutputCacheEntry(KnownTime, StatusCodes.Status201Created))
        {
            entry.CopyHeadersFrom(new HeaderDictionary { [""] = "", [HeaderNames.Accept] = new[] { null, null, "", "text/html" }, [HeaderNames.AcceptCharset] = new string[] { null } });

            await OutputCacheEntryFormatter.StoreAsync(key, entry, null, TimeSpan.Zero, store, NullLogger.Instance, default);
        }
        var payload = await store.GetAsync(key, CancellationToken.None);
        Assert.NotNull(payload);
        var hex = BitConverter.ToString(payload);
        Assert.Equal(KnownV2Payload, hex);

        var result = await OutputCacheEntryFormatter.GetAsync(key, store, default);

        Assert.Equal(3, result.Headers.Length);
        Assert.True(result.TryFindHeader("", out var values), "Find ''");
        Assert.Equal("", values);
        Assert.True(result.TryFindHeader(HeaderNames.Accept, out values));
        Assert.Equal(4, values.Count);
        Assert.Equal("", values[0]);
        Assert.Equal("", values[1]);
        Assert.Equal("", values[2]);
        Assert.Equal("text/html", values[3]);
        Assert.True(result.TryFindHeader(HeaderNames.AcceptCharset, out values), "Find 'AcceptCharset'");
        Assert.Equal("", values[0]);
    }

    [Fact]
    public void KnownV1AndV2AreCompatible()
    {
        AssertEntriesAreSame(
            OutputCacheEntryFormatter.Deserialize(FromHex(KnownV1Payload)),
            OutputCacheEntryFormatter.Deserialize(FromHex(KnownV2Payload))
        );
    }
    static byte[] FromHex(string hex)
    {
        // inefficient; for testing only
        hex = hex.Replace("-", "");
        var arr = new byte[hex.Length / 2];
        int index = 0;
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = (byte)((Nibble(hex[index++]) << 4) | Nibble(hex[index++]));
        }
        return arr;

        static int Nibble(char value)
        {
            return value switch
            {
                >= '0' and <= '9' => value - '0',
                >= 'a' and <= 'f' => value - 'a' + 10,
                >= 'A' and <= 'F' => value - 'A' + 10,
                _ => throw new ArgumentOutOfRangeException(nameof(value), "token is not hex: " + value.ToString())
            };
        }
    }

    const string KnownV1Payload = "01-B0-E8-8E-B2-95-D9-D5-ED-08-00-C9-01-03-00-01-00-06-41-63-63-65-70-74-04-00-00-00-09-74-65-78-74-2F-68-74-6D-6C-0E-41-63-63-65-70-74-2D-43-68-61-72-73-65-74-01-00-00-00";
    // 01                                              version 1
    // B0-E8-8E-B2-95-D9-D5-ED-08                      ticks 1684322693875
    // 00                                              offset 0
    // C9-01                                           status 201
    // 03                                              headers 3
    // 00                                              [0] header name ""
    // 01                                              [0] header value count 1
    // 00                                              [0.0] header value ""
    // 06-41-63-63-65-70-74                            [1] header name "Accept"
    // 04                                              [1] header value count 4
    // 00-00-00                                        [1.0, 1.1, 1.2] header value ""
    // 09-74-65-78-74-2F-68-74-6D-6C                   [1.3] header value "text/html"
    // 0E-41-63-63-65-70-74-2D-43-68-61-72-73-65-74    [2] header name "Accept-Charset"
    // 01                                              [2] header value count 1
    // 00                                              [2.0] header value ""
    // 00                                              segment count 0
    // 00                                              tag count 0

    const string KnownV2Payload = "02-B0-E8-8E-B2-95-D9-D5-ED-08-00-C9-01-03-00-01-00-01-04-00-00-00-9B-01-03-01-00-00";
    // 02                                              version 2
    // B0-E8-8E-B2-95-D9-D5-ED-08                      ticks 1684322693875
    // 00                                              offset 0
    // C9-01                                           status 201
    // 03                                              headers 3
    // 00                                              [0] header name ""
    // 01                                              [0] header value count 1
    // 00                                              [0.0] header value ""
    // 01                                              [1] header name "Accept"
    // 04                                              [1] header value count 4
    // 00-00-00                                        [1.0, 1.1, 1.2] header value ""
    // 9B-01                                           [1.3] header value "text/html"
    // 03                                              [2] header name "Accept-Charset"
    // 01                                              [2] header value count 1
    // 00                                              [2.0] header value ""
    // 00                                              segment count 0

    private static void AssertEntriesAreSame(OutputCacheEntry expected, OutputCacheEntry actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);
        Assert.Equal(expected.Created, actual.Created);
        Assert.Equal(expected.StatusCode, actual.StatusCode);
        Assert.True(expected.Headers.Span.SequenceEqual(actual.Headers.Span), "Headers");
        Assert.Equal(expected.Body.Length, actual.Body.Length);
        Assert.True(SequenceEqual(expected.Body, actual.Body));
    }

    static bool SequenceEqual(ReadOnlySequence<byte> x, ReadOnlySequence<byte> y)
    {
        var xLinear = Linearize(x, out var xBuffer);
        var yLinear = Linearize(x, out var yBuffer);

        var result = xLinear.Span.SequenceEqual(yLinear.Span);

        if (xBuffer is not null)
        {
            ArrayPool<byte>.Shared.Return(xBuffer);
        }
        if (yBuffer is not null)
        {
            ArrayPool<byte>.Shared.Return(yBuffer);
        }

        return result;

        static ReadOnlyMemory<byte> Linearize(in ReadOnlySequence<byte> value, out byte[] lease)
        {
            lease = null;
            if (value.IsEmpty) { return default; }
            if (value.IsSingleSegment) { return value.First; }

            var len = checked((int)value.Length);
            lease = ArrayPool<byte>.Shared.Rent(len);
            value.CopyTo(lease);
            return new ReadOnlyMemory<byte>(lease, 0, len);
        }
    }
}
