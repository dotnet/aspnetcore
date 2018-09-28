// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class HttpHeadersTests
    {
        [Theory]
        [InlineData("", ConnectionOptions.None)]
        [InlineData(",", ConnectionOptions.None)]
        [InlineData(" ,", ConnectionOptions.None)]
        [InlineData(" , ", ConnectionOptions.None)]
        [InlineData(",,", ConnectionOptions.None)]
        [InlineData(" ,,", ConnectionOptions.None)]
        [InlineData(",, ", ConnectionOptions.None)]
        [InlineData(" , ,", ConnectionOptions.None)]
        [InlineData(" , , ", ConnectionOptions.None)]
        [InlineData("keep-alive", ConnectionOptions.KeepAlive)]
        [InlineData("keep-alive, upgrade", ConnectionOptions.KeepAlive | ConnectionOptions.Upgrade)]
        [InlineData("keep-alive,upgrade", ConnectionOptions.KeepAlive | ConnectionOptions.Upgrade)]
        [InlineData("upgrade, keep-alive", ConnectionOptions.KeepAlive | ConnectionOptions.Upgrade)]
        [InlineData("upgrade,keep-alive", ConnectionOptions.KeepAlive | ConnectionOptions.Upgrade)]
        [InlineData("upgrade,,keep-alive", ConnectionOptions.KeepAlive | ConnectionOptions.Upgrade)]
        [InlineData("keep-alive,", ConnectionOptions.KeepAlive)]
        [InlineData("keep-alive,,", ConnectionOptions.KeepAlive)]
        [InlineData("keep-alive, ", ConnectionOptions.KeepAlive)]
        [InlineData("keep-alive, ,", ConnectionOptions.KeepAlive)]
        [InlineData("keep-alive, , ", ConnectionOptions.KeepAlive)]
        [InlineData("keep-alive ,", ConnectionOptions.KeepAlive)]
        [InlineData(",keep-alive", ConnectionOptions.KeepAlive)]
        [InlineData(", keep-alive", ConnectionOptions.KeepAlive)]
        [InlineData(",,keep-alive", ConnectionOptions.KeepAlive)]
        [InlineData(", ,keep-alive", ConnectionOptions.KeepAlive)]
        [InlineData(",, keep-alive", ConnectionOptions.KeepAlive)]
        [InlineData(", , keep-alive", ConnectionOptions.KeepAlive)]
        [InlineData("upgrade,", ConnectionOptions.Upgrade)]
        [InlineData("upgrade,,", ConnectionOptions.Upgrade)]
        [InlineData("upgrade, ", ConnectionOptions.Upgrade)]
        [InlineData("upgrade, ,", ConnectionOptions.Upgrade)]
        [InlineData("upgrade, , ", ConnectionOptions.Upgrade)]
        [InlineData("upgrade ,", ConnectionOptions.Upgrade)]
        [InlineData(",upgrade", ConnectionOptions.Upgrade)]
        [InlineData(", upgrade", ConnectionOptions.Upgrade)]
        [InlineData(",,upgrade", ConnectionOptions.Upgrade)]
        [InlineData(", ,upgrade", ConnectionOptions.Upgrade)]
        [InlineData(",, upgrade", ConnectionOptions.Upgrade)]
        [InlineData(", , upgrade", ConnectionOptions.Upgrade)]
        [InlineData("close,", ConnectionOptions.Close)]
        [InlineData("close,,", ConnectionOptions.Close)]
        [InlineData("close, ", ConnectionOptions.Close)]
        [InlineData("close, ,", ConnectionOptions.Close)]
        [InlineData("close, , ", ConnectionOptions.Close)]
        [InlineData("close ,", ConnectionOptions.Close)]
        [InlineData(",close", ConnectionOptions.Close)]
        [InlineData(", close", ConnectionOptions.Close)]
        [InlineData(",,close", ConnectionOptions.Close)]
        [InlineData(", ,close", ConnectionOptions.Close)]
        [InlineData(",, close", ConnectionOptions.Close)]
        [InlineData(", , close", ConnectionOptions.Close)]
        [InlineData("kupgrade", ConnectionOptions.None)]
        [InlineData("keupgrade", ConnectionOptions.None)]
        [InlineData("ukeep-alive", ConnectionOptions.None)]
        [InlineData("upkeep-alive", ConnectionOptions.None)]
        [InlineData("k,upgrade", ConnectionOptions.Upgrade)]
        [InlineData("u,keep-alive", ConnectionOptions.KeepAlive)]
        [InlineData("ke,upgrade", ConnectionOptions.Upgrade)]
        [InlineData("up,keep-alive", ConnectionOptions.KeepAlive)]
        [InlineData("close", ConnectionOptions.Close)]
        [InlineData("upgrade,close", ConnectionOptions.Close | ConnectionOptions.Upgrade)]
        [InlineData("close,upgrade", ConnectionOptions.Close | ConnectionOptions.Upgrade)]
        [InlineData("keep-alive2", ConnectionOptions.None)]
        [InlineData("keep-alive2 ", ConnectionOptions.None)]
        [InlineData("keep-alive2 ,", ConnectionOptions.None)]
        [InlineData("keep-alive2,", ConnectionOptions.None)]
        [InlineData("upgrade2", ConnectionOptions.None)]
        [InlineData("upgrade2 ", ConnectionOptions.None)]
        [InlineData("upgrade2 ,", ConnectionOptions.None)]
        [InlineData("upgrade2,", ConnectionOptions.None)]
        [InlineData("close2", ConnectionOptions.None)]
        [InlineData("close2 ", ConnectionOptions.None)]
        [InlineData("close2 ,", ConnectionOptions.None)]
        [InlineData("close2,", ConnectionOptions.None)]
        [InlineData("keep-alivekeep-alive", ConnectionOptions.None)]
        [InlineData("keep-aliveupgrade", ConnectionOptions.None)]
        [InlineData("upgradeupgrade", ConnectionOptions.None)]
        [InlineData("upgradekeep-alive", ConnectionOptions.None)]
        [InlineData("closeclose", ConnectionOptions.None)]
        [InlineData("closeupgrade", ConnectionOptions.None)]
        [InlineData("upgradeclose", ConnectionOptions.None)]
        [InlineData("keep-alive 2", ConnectionOptions.None)]
        [InlineData("upgrade 2", ConnectionOptions.None)]
        [InlineData("keep-alive 2, close", ConnectionOptions.Close)]
        [InlineData("upgrade 2, close", ConnectionOptions.Close)]
        [InlineData("close, keep-alive 2", ConnectionOptions.Close)]
        [InlineData("close, upgrade 2", ConnectionOptions.Close)]
        [InlineData("close 2, upgrade", ConnectionOptions.Upgrade)]
        [InlineData("upgrade, close 2", ConnectionOptions.Upgrade)]
        [InlineData("k2ep-alive", ConnectionOptions.None)]
        [InlineData("ke2p-alive", ConnectionOptions.None)]
        [InlineData("u2grade", ConnectionOptions.None)]
        [InlineData("up2rade", ConnectionOptions.None)]
        [InlineData("c2ose", ConnectionOptions.None)]
        [InlineData("cl2se", ConnectionOptions.None)]
        [InlineData("k2ep-alive,", ConnectionOptions.None)]
        [InlineData("ke2p-alive,", ConnectionOptions.None)]
        [InlineData("u2grade,", ConnectionOptions.None)]
        [InlineData("up2rade,", ConnectionOptions.None)]
        [InlineData("c2ose,", ConnectionOptions.None)]
        [InlineData("cl2se,", ConnectionOptions.None)]
        [InlineData("k2ep-alive ", ConnectionOptions.None)]
        [InlineData("ke2p-alive ", ConnectionOptions.None)]
        [InlineData("u2grade ", ConnectionOptions.None)]
        [InlineData("up2rade ", ConnectionOptions.None)]
        [InlineData("c2ose ", ConnectionOptions.None)]
        [InlineData("cl2se ", ConnectionOptions.None)]
        [InlineData("k2ep-alive ,", ConnectionOptions.None)]
        [InlineData("ke2p-alive ,", ConnectionOptions.None)]
        [InlineData("u2grade ,", ConnectionOptions.None)]
        [InlineData("up2rade ,", ConnectionOptions.None)]
        [InlineData("c2ose ,", ConnectionOptions.None)]
        [InlineData("cl2se ,", ConnectionOptions.None)]
        public void TestParseConnection(string connection, ConnectionOptions expectedConnectionOptions)
        {
            var connectionOptions = HttpHeaders.ParseConnection(connection);
            Assert.Equal(expectedConnectionOptions, connectionOptions);
        }

        [Theory]
        [InlineData("keep-alive", "upgrade", ConnectionOptions.KeepAlive | ConnectionOptions.Upgrade)]
        [InlineData("upgrade", "keep-alive", ConnectionOptions.KeepAlive | ConnectionOptions.Upgrade)]
        [InlineData("keep-alive", "", ConnectionOptions.KeepAlive)]
        [InlineData("", "keep-alive", ConnectionOptions.KeepAlive)]
        [InlineData("upgrade", "", ConnectionOptions.Upgrade)]
        [InlineData("", "upgrade", ConnectionOptions.Upgrade)]
        [InlineData("keep-alive, upgrade", "", ConnectionOptions.KeepAlive | ConnectionOptions.Upgrade)]
        [InlineData("upgrade, keep-alive", "", ConnectionOptions.KeepAlive | ConnectionOptions.Upgrade)]
        [InlineData("", "keep-alive, upgrade", ConnectionOptions.KeepAlive | ConnectionOptions.Upgrade)]
        [InlineData("", "upgrade, keep-alive", ConnectionOptions.KeepAlive | ConnectionOptions.Upgrade)]
        [InlineData("", "", ConnectionOptions.None)]
        [InlineData("close", "", ConnectionOptions.Close)]
        [InlineData("", "close", ConnectionOptions.Close)]
        [InlineData("close", "upgrade", ConnectionOptions.Close | ConnectionOptions.Upgrade)]
        [InlineData("upgrade", "close", ConnectionOptions.Close | ConnectionOptions.Upgrade)]
        public void TestParseConnectionMultipleValues(string value1, string value2, ConnectionOptions expectedConnectionOptions)
        {
            var connection = new StringValues(new[] { value1, value2 });
            var connectionOptions = HttpHeaders.ParseConnection(connection);
            Assert.Equal(expectedConnectionOptions, connectionOptions);
        }

        [Theory]
        [InlineData("", TransferCoding.None)]
        [InlineData(",,", TransferCoding.None)]
        [InlineData(" ,,", TransferCoding.None)]
        [InlineData(",, ", TransferCoding.None)]
        [InlineData(" , ,", TransferCoding.None)]
        [InlineData(" , , ", TransferCoding.None)]
        [InlineData("chunked,", TransferCoding.Chunked)]
        [InlineData("chunked,,", TransferCoding.Chunked)]
        [InlineData("chunked, ", TransferCoding.Chunked)]
        [InlineData("chunked, ,", TransferCoding.Chunked)]
        [InlineData("chunked, , ", TransferCoding.Chunked)]
        [InlineData("chunked ,", TransferCoding.Chunked)]
        [InlineData(",chunked", TransferCoding.Chunked)]
        [InlineData(", chunked", TransferCoding.Chunked)]
        [InlineData(",,chunked", TransferCoding.Chunked)]
        [InlineData(", ,chunked", TransferCoding.Chunked)]
        [InlineData(",, chunked", TransferCoding.Chunked)]
        [InlineData(", , chunked", TransferCoding.Chunked)]
        [InlineData("chunked, gzip", TransferCoding.Other)]
        [InlineData("chunked,compress", TransferCoding.Other)]
        [InlineData("deflate, chunked", TransferCoding.Chunked)]
        [InlineData("gzip,chunked", TransferCoding.Chunked)]
        [InlineData("compress,,chunked", TransferCoding.Chunked)]
        [InlineData("chunkedchunked", TransferCoding.Other)]
        [InlineData("chunked2", TransferCoding.Other)]
        [InlineData("chunked 2", TransferCoding.Other)]
        [InlineData("2chunked", TransferCoding.Other)]
        [InlineData("c2unked", TransferCoding.Other)]
        [InlineData("ch2nked", TransferCoding.Other)]
        [InlineData("chunked 2, gzip", TransferCoding.Other)]
        [InlineData("chunked2, gzip", TransferCoding.Other)]
        [InlineData("gzip, chunked 2", TransferCoding.Other)]
        [InlineData("gzip, chunked2", TransferCoding.Other)]
        public void TestParseTransferEncoding(string transferEncoding, TransferCoding expectedTransferEncodingOptions)
        {
            var transferEncodingOptions = HttpHeaders.GetFinalTransferCoding(transferEncoding);
            Assert.Equal(expectedTransferEncodingOptions, transferEncodingOptions);
        }

        [Theory]
        [InlineData("chunked", "gzip", TransferCoding.Other)]
        [InlineData("compress", "chunked", TransferCoding.Chunked)]
        [InlineData("chunked", "", TransferCoding.Chunked)]
        [InlineData("", "chunked", TransferCoding.Chunked)]
        [InlineData("chunked, deflate", "", TransferCoding.Other)]
        [InlineData("gzip, chunked", "", TransferCoding.Chunked)]
        [InlineData("", "chunked, compress", TransferCoding.Other)]
        [InlineData("", "compress, chunked", TransferCoding.Chunked)]
        [InlineData("", "", TransferCoding.None)]
        [InlineData("deflate", "", TransferCoding.Other)]
        [InlineData("", "gzip", TransferCoding.Other)]
        public void TestParseTransferEncodingMultipleValues(string value1, string value2, TransferCoding expectedTransferEncodingOptions)
        {
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
    }
}
