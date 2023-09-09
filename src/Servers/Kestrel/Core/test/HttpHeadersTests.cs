// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class HttpHeadersTests
{
    [Theory]
    [InlineData("", (int)(ConnectionOptions.None))]
    [InlineData(",", (int)(ConnectionOptions.None))]
    [InlineData(" ,", (int)(ConnectionOptions.None))]
    [InlineData(" , ", (int)(ConnectionOptions.None))]
    [InlineData(",,", (int)(ConnectionOptions.None))]
    [InlineData(" ,,", (int)(ConnectionOptions.None))]
    [InlineData(",, ", (int)(ConnectionOptions.None))]
    [InlineData(" , ,", (int)(ConnectionOptions.None))]
    [InlineData(" , , ", (int)(ConnectionOptions.None))]
    [InlineData("KEEP-ALIVE", (int)(ConnectionOptions.KeepAlive))]
    [InlineData("keep-alive", (int)(ConnectionOptions.KeepAlive))]
    [InlineData("keep-alive, upgrade", (int)(ConnectionOptions.KeepAlive | ConnectionOptions.Upgrade))]
    [InlineData("keep-alive,upgrade", (int)(ConnectionOptions.KeepAlive | ConnectionOptions.Upgrade))]
    [InlineData("upgrade, keep-alive", (int)(ConnectionOptions.KeepAlive | ConnectionOptions.Upgrade))]
    [InlineData("upgrade,keep-alive", (int)(ConnectionOptions.KeepAlive | ConnectionOptions.Upgrade))]
    [InlineData("upgrade,,keep-alive", (int)(ConnectionOptions.KeepAlive | ConnectionOptions.Upgrade))]
    [InlineData("keep-alive,", (int)(ConnectionOptions.KeepAlive))]
    [InlineData("keep-alive,,", (int)(ConnectionOptions.KeepAlive))]
    [InlineData("keep-alive, ", (int)(ConnectionOptions.KeepAlive))]
    [InlineData("keep-alive, ,", (int)(ConnectionOptions.KeepAlive))]
    [InlineData("keep-alive, , ", (int)(ConnectionOptions.KeepAlive))]
    [InlineData("keep-alive ,", (int)(ConnectionOptions.KeepAlive))]
    [InlineData(",keep-alive", (int)(ConnectionOptions.KeepAlive))]
    [InlineData(", keep-alive", (int)(ConnectionOptions.KeepAlive))]
    [InlineData(",,keep-alive", (int)(ConnectionOptions.KeepAlive))]
    [InlineData(", ,keep-alive", (int)(ConnectionOptions.KeepAlive))]
    [InlineData(",, keep-alive", (int)(ConnectionOptions.KeepAlive))]
    [InlineData(", , keep-alive", (int)(ConnectionOptions.KeepAlive))]
    [InlineData("UPGRADE", (int)(ConnectionOptions.Upgrade))]
    [InlineData("upgrade", (int)(ConnectionOptions.Upgrade))]
    [InlineData("upgrade,", (int)(ConnectionOptions.Upgrade))]
    [InlineData("upgrade,,", (int)(ConnectionOptions.Upgrade))]
    [InlineData("upgrade, ", (int)(ConnectionOptions.Upgrade))]
    [InlineData("upgrade, ,", (int)(ConnectionOptions.Upgrade))]
    [InlineData("upgrade, , ", (int)(ConnectionOptions.Upgrade))]
    [InlineData("upgrade ,", (int)(ConnectionOptions.Upgrade))]
    [InlineData(",upgrade", (int)(ConnectionOptions.Upgrade))]
    [InlineData(", upgrade", (int)(ConnectionOptions.Upgrade))]
    [InlineData(",,upgrade", (int)(ConnectionOptions.Upgrade))]
    [InlineData(", ,upgrade", (int)(ConnectionOptions.Upgrade))]
    [InlineData(",, upgrade", (int)(ConnectionOptions.Upgrade))]
    [InlineData(", , upgrade", (int)(ConnectionOptions.Upgrade))]
    [InlineData("close,", (int)(ConnectionOptions.Close))]
    [InlineData("close,,", (int)(ConnectionOptions.Close))]
    [InlineData("close, ", (int)(ConnectionOptions.Close))]
    [InlineData("close, ,", (int)(ConnectionOptions.Close))]
    [InlineData("close, , ", (int)(ConnectionOptions.Close))]
    [InlineData("close ,", (int)(ConnectionOptions.Close))]
    [InlineData(",close", (int)(ConnectionOptions.Close))]
    [InlineData(", close", (int)(ConnectionOptions.Close))]
    [InlineData(",,close", (int)(ConnectionOptions.Close))]
    [InlineData(", ,close", (int)(ConnectionOptions.Close))]
    [InlineData(",, close", (int)(ConnectionOptions.Close))]
    [InlineData(", , close", (int)(ConnectionOptions.Close))]
    [InlineData("kupgrade", (int)(ConnectionOptions.None))]
    [InlineData("keupgrade", (int)(ConnectionOptions.None))]
    [InlineData("ukeep-alive", (int)(ConnectionOptions.None))]
    [InlineData("upkeep-alive", (int)(ConnectionOptions.None))]
    [InlineData("k,upgrade", (int)(ConnectionOptions.Upgrade))]
    [InlineData("u,keep-alive", (int)(ConnectionOptions.KeepAlive))]
    [InlineData("ke,upgrade", (int)(ConnectionOptions.Upgrade))]
    [InlineData("up,keep-alive", (int)(ConnectionOptions.KeepAlive))]
    [InlineData("CLOSE", (int)(ConnectionOptions.Close))]
    [InlineData("close", (int)(ConnectionOptions.Close))]
    [InlineData("upgrade,close", (int)(ConnectionOptions.Close | ConnectionOptions.Upgrade))]
    [InlineData("close,upgrade", (int)(ConnectionOptions.Close | ConnectionOptions.Upgrade))]
    [InlineData("keep-alive2", (int)(ConnectionOptions.None))]
    [InlineData("keep-alive2 ", (int)(ConnectionOptions.None))]
    [InlineData("keep-alive2 ,", (int)(ConnectionOptions.None))]
    [InlineData("keep-alive2,", (int)(ConnectionOptions.None))]
    [InlineData("upgrade2", (int)(ConnectionOptions.None))]
    [InlineData("upgrade2 ", (int)(ConnectionOptions.None))]
    [InlineData("upgrade2 ,", (int)(ConnectionOptions.None))]
    [InlineData("upgrade2,", (int)(ConnectionOptions.None))]
    [InlineData("close2", (int)(ConnectionOptions.None))]
    [InlineData("close2 ", (int)(ConnectionOptions.None))]
    [InlineData("close2 ,", (int)(ConnectionOptions.None))]
    [InlineData("close2,", (int)(ConnectionOptions.None))]
    [InlineData("close close", (int)(ConnectionOptions.None))]
    [InlineData("close dclose", (int)(ConnectionOptions.None))]
    [InlineData("keep-alivekeep-alive", (int)(ConnectionOptions.None))]
    [InlineData("keep-aliveupgrade", (int)(ConnectionOptions.None))]
    [InlineData("upgradeupgrade", (int)(ConnectionOptions.None))]
    [InlineData("upgradekeep-alive", (int)(ConnectionOptions.None))]
    [InlineData("closeclose", (int)(ConnectionOptions.None))]
    [InlineData("closeupgrade", (int)(ConnectionOptions.None))]
    [InlineData("upgradeclose", (int)(ConnectionOptions.None))]
    [InlineData("keep-alive 2", (int)(ConnectionOptions.None))]
    [InlineData("upgrade 2", (int)(ConnectionOptions.None))]
    [InlineData("keep-alive 2, close", (int)(ConnectionOptions.Close))]
    [InlineData("upgrade 2, close", (int)(ConnectionOptions.Close))]
    [InlineData("close, keep-alive 2", (int)(ConnectionOptions.Close))]
    [InlineData("close, upgrade 2", (int)(ConnectionOptions.Close))]
    [InlineData("close 2, upgrade", (int)(ConnectionOptions.Upgrade))]
    [InlineData("upgrade, close 2", (int)(ConnectionOptions.Upgrade))]
    [InlineData("k2ep-alive", (int)(ConnectionOptions.None))]
    [InlineData("ke2p-alive", (int)(ConnectionOptions.None))]
    [InlineData("u2grade", (int)(ConnectionOptions.None))]
    [InlineData("up2rade", (int)(ConnectionOptions.None))]
    [InlineData("c2ose", (int)(ConnectionOptions.None))]
    [InlineData("cl2se", (int)(ConnectionOptions.None))]
    [InlineData("k2ep-alive,", (int)(ConnectionOptions.None))]
    [InlineData("ke2p-alive,", (int)(ConnectionOptions.None))]
    [InlineData("u2grade,", (int)(ConnectionOptions.None))]
    [InlineData("up2rade,", (int)(ConnectionOptions.None))]
    [InlineData("c2ose,", (int)(ConnectionOptions.None))]
    [InlineData("cl2se,", (int)(ConnectionOptions.None))]
    [InlineData("k2ep-alive ", (int)(ConnectionOptions.None))]
    [InlineData("ke2p-alive ", (int)(ConnectionOptions.None))]
    [InlineData("u2grade ", (int)(ConnectionOptions.None))]
    [InlineData("up2rade ", (int)(ConnectionOptions.None))]
    [InlineData("c2ose ", (int)(ConnectionOptions.None))]
    [InlineData("cl2se ", (int)(ConnectionOptions.None))]
    [InlineData("k2ep-alive ,", (int)(ConnectionOptions.None))]
    [InlineData("ke2p-alive ,", (int)(ConnectionOptions.None))]
    [InlineData("u2grade ,", (int)(ConnectionOptions.None))]
    [InlineData("up2rade ,", (int)(ConnectionOptions.None))]
    [InlineData("c2ose ,", (int)(ConnectionOptions.None))]
    [InlineData("cl2se ,", (int)(ConnectionOptions.None))]
    public void TestParseConnection(string connection, int intExpectedConnectionOptions)
    {
        var expectedConnectionOptions = (ConnectionOptions)intExpectedConnectionOptions;
        var requestHeaders = new HttpRequestHeaders();
        requestHeaders.HeaderConnection = connection;
        var connectionOptions = HttpHeaders.ParseConnection(requestHeaders);
        Assert.Equal(expectedConnectionOptions, connectionOptions);
    }

    [Theory]
    [InlineData("keep-alive", "upgrade", (int)(ConnectionOptions.KeepAlive | ConnectionOptions.Upgrade))]
    [InlineData("upgrade", "keep-alive", (int)(ConnectionOptions.KeepAlive | ConnectionOptions.Upgrade))]
    [InlineData("keep-alive", "", (int)(ConnectionOptions.KeepAlive))]
    [InlineData("", "keep-alive", (int)(ConnectionOptions.KeepAlive))]
    [InlineData("upgrade", "", (int)(ConnectionOptions.Upgrade))]
    [InlineData("", "upgrade", (int)(ConnectionOptions.Upgrade))]
    [InlineData("keep-alive, upgrade", "", (int)(ConnectionOptions.KeepAlive | ConnectionOptions.Upgrade))]
    [InlineData("upgrade, keep-alive", "", (int)(ConnectionOptions.KeepAlive | ConnectionOptions.Upgrade))]
    [InlineData("", "keep-alive, upgrade", (int)(ConnectionOptions.KeepAlive | ConnectionOptions.Upgrade))]
    [InlineData("", "upgrade, keep-alive", (int)(ConnectionOptions.KeepAlive | ConnectionOptions.Upgrade))]
    [InlineData("", "", (int)(ConnectionOptions.None))]
    [InlineData("close", "", (int)(ConnectionOptions.Close))]
    [InlineData("", "close", (int)(ConnectionOptions.Close))]
    [InlineData("close", "upgrade", (int)(ConnectionOptions.Close | ConnectionOptions.Upgrade))]
    [InlineData("upgrade", "close", (int)(ConnectionOptions.Close | ConnectionOptions.Upgrade))]
    public void TestParseConnectionMultipleValues(string value1, string value2, int intExpectedConnectionOptions)
    {
        var expectedConnectionOptions = (ConnectionOptions)intExpectedConnectionOptions;
        var connection = new StringValues(new[] { value1, value2 });
        var requestHeaders = new HttpRequestHeaders();
        requestHeaders.HeaderConnection = connection;
        var connectionOptions = HttpHeaders.ParseConnection(requestHeaders);
        Assert.Equal(expectedConnectionOptions, connectionOptions);
    }

    [Theory]
    [InlineData("", (int)(TransferCoding.None))]
    [InlineData(",,", (int)(TransferCoding.None))]
    [InlineData(" ,,", (int)(TransferCoding.None))]
    [InlineData(",, ", (int)(TransferCoding.None))]
    [InlineData(" , ,", (int)(TransferCoding.None))]
    [InlineData(" , , ", (int)(TransferCoding.None))]
    [InlineData("c", (int)(TransferCoding.Other))]
    [InlineData("z", (int)(TransferCoding.Other))]
    [InlineData("chunk", (int)(TransferCoding.Other))]
    [InlineData("chunked,", (int)(TransferCoding.Chunked))]
    [InlineData("chunked,,", (int)(TransferCoding.Chunked))]
    [InlineData("chunked, ", (int)(TransferCoding.Chunked))]
    [InlineData("chunked, ,", (int)(TransferCoding.Chunked))]
    [InlineData("chunked, , ", (int)(TransferCoding.Chunked))]
    [InlineData("chunked ,", (int)(TransferCoding.Chunked))]
    [InlineData(",chunked", (int)(TransferCoding.Chunked))]
    [InlineData(", chunked", (int)(TransferCoding.Chunked))]
    [InlineData(",,chunked", (int)(TransferCoding.Chunked))]
    [InlineData(", ,chunked", (int)(TransferCoding.Chunked))]
    [InlineData(",, chunked", (int)(TransferCoding.Chunked))]
    [InlineData(", , chunked", (int)(TransferCoding.Chunked))]
    [InlineData("chunked, gzip", (int)(TransferCoding.Other))]
    [InlineData("chunked,compress", (int)(TransferCoding.Other))]
    [InlineData("deflate, chunked", (int)(TransferCoding.Chunked))]
    [InlineData("gzip,chunked", (int)(TransferCoding.Chunked))]
    [InlineData("compress,,chunked", (int)(TransferCoding.Chunked))]
    [InlineData("chunked,c", (int)(TransferCoding.Other))]
    [InlineData("chunked,z", (int)(TransferCoding.Other))]
    [InlineData("chunked,zz", (int)(TransferCoding.Other))]
    [InlineData("chunked, z", (int)(TransferCoding.Other))]
    [InlineData("chunked, zz", (int)(TransferCoding.Other))]
    [InlineData("chunked,chunk", (int)(TransferCoding.Other))]
    [InlineData("z,chunked", (int)(TransferCoding.Chunked))]
    [InlineData("z, chunked", (int)(TransferCoding.Chunked))]
    [InlineData("chunkedchunked", (int)(TransferCoding.Other))]
    [InlineData("chunked2", (int)(TransferCoding.Other))]
    [InlineData("chunked 2", (int)(TransferCoding.Other))]
    [InlineData("2chunked", (int)(TransferCoding.Other))]
    [InlineData("c2unked", (int)(TransferCoding.Other))]
    [InlineData("ch2nked", (int)(TransferCoding.Other))]
    [InlineData("chunked 2, gzip", (int)(TransferCoding.Other))]
    [InlineData("chunked2, gzip", (int)(TransferCoding.Other))]
    [InlineData("gzip, chunked 2", (int)(TransferCoding.Other))]
    [InlineData("gzip, chunked2", (int)(TransferCoding.Other))]
    public void TestParseTransferEncoding(string transferEncoding, int intExpectedTransferEncodingOptions)
    {
        var expectedTransferEncodingOptions = (TransferCoding)intExpectedTransferEncodingOptions;

        var transferEncodingOptions = HttpHeaders.GetFinalTransferCoding(transferEncoding);
        Assert.Equal(expectedTransferEncodingOptions, transferEncodingOptions);
    }

    [Theory]
    [InlineData("chunked", "gzip", (int)(TransferCoding.Other))]
    [InlineData("compress", "chunked", (int)(TransferCoding.Chunked))]
    [InlineData("chunked", "", (int)(TransferCoding.Chunked))]
    [InlineData("", "chunked", (int)(TransferCoding.Chunked))]
    [InlineData("chunked, deflate", "", (int)(TransferCoding.Other))]
    [InlineData("gzip, chunked", "", (int)(TransferCoding.Chunked))]
    [InlineData("", "chunked, compress", (int)(TransferCoding.Other))]
    [InlineData("", "compress, chunked", (int)(TransferCoding.Chunked))]
    [InlineData("", "", (int)(TransferCoding.None))]
    [InlineData("deflate", "", (int)(TransferCoding.Other))]
    [InlineData("", "gzip", (int)(TransferCoding.Other))]
    public void TestParseTransferEncodingMultipleValues(string value1, string value2, int intExpectedTransferEncodingOptions)
    {
        var expectedTransferEncodingOptions = (TransferCoding)intExpectedTransferEncodingOptions;

        var transferEncoding = new StringValues(new[] { value1, value2 });
        var transferEncodingOptions = HttpHeaders.GetFinalTransferCoding(transferEncoding);
        Assert.Equal(expectedTransferEncodingOptions, transferEncodingOptions);
    }

    [Fact]
    public void ValidContentLengthsAccepted()
    {
        ValidContentLengthsAcceptedImpl(new HttpRequestHeaders());
        ValidContentLengthsAcceptedImpl(new HttpResponseHeaders());
    }

    private static void ValidContentLengthsAcceptedImpl(HttpHeaders httpHeaders)
    {
        IDictionary<string, StringValues> headers = httpHeaders;

        Assert.False(headers.TryGetValue("Content-Length", out var value));
        Assert.Null(httpHeaders.ContentLength);
        Assert.False(httpHeaders.ContentLength.HasValue);

        httpHeaders.ContentLength = 1;
        Assert.True(headers.TryGetValue("Content-Length", out value));
        Assert.Equal("1", value[0]);
        Assert.Equal(1, httpHeaders.ContentLength);
        Assert.True(httpHeaders.ContentLength.HasValue);

        httpHeaders.ContentLength = long.MaxValue;
        Assert.True(headers.TryGetValue("Content-Length", out value));
        Assert.Equal(HeaderUtilities.FormatNonNegativeInt64(long.MaxValue), value[0]);
        Assert.Equal(long.MaxValue, httpHeaders.ContentLength);
        Assert.True(httpHeaders.ContentLength.HasValue);

        httpHeaders.ContentLength = null;
        Assert.False(headers.TryGetValue("Content-Length", out value));
        Assert.Null(httpHeaders.ContentLength);
        Assert.False(httpHeaders.ContentLength.HasValue);
    }

    [Fact]
    public void InvalidContentLengthsRejected()
    {
        InvalidContentLengthsRejectedImpl(new HttpRequestHeaders());
        InvalidContentLengthsRejectedImpl(new HttpResponseHeaders());
    }

    private static void InvalidContentLengthsRejectedImpl(HttpHeaders httpHeaders)
    {
        IDictionary<string, StringValues> headers = httpHeaders;

        StringValues value;

        Assert.False(headers.TryGetValue("Content-Length", out value));
        Assert.Null(httpHeaders.ContentLength);
        Assert.False(httpHeaders.ContentLength.HasValue);

        Assert.Throws<ArgumentOutOfRangeException>(() => httpHeaders.ContentLength = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => httpHeaders.ContentLength = long.MinValue);

        Assert.False(headers.TryGetValue("Content-Length", out value));
        Assert.Null(httpHeaders.ContentLength);
        Assert.False(httpHeaders.ContentLength.HasValue);
    }

    [Fact]
    public void KeysCompareShouldBeCaseInsensitive()
    {
        var httpHeaders = (IHeaderDictionary)new HttpRequestHeaders();
        httpHeaders["Cache-Control"] = "no-cache";
        Assert.True(httpHeaders.Keys.Contains("cache-control"));
    }
}
