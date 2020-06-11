// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class HttpResponseHeadersTests
    {
        [Fact]
        public void InitialDictionaryIsEmpty()
        {
            using (var memoryPool = SlabMemoryPoolFactory.Create())
            {
                var options = new PipeOptions(memoryPool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);
                var pair = DuplexPipe.CreateConnectionPair(options, options);
                var http1ConnectionContext = new HttpConnectionContext
                {
                    ServiceContext = new TestServiceContext(),
                    ConnectionFeatures = new FeatureCollection(),
                    MemoryPool = memoryPool,
                    Transport = pair.Transport,
                    TimeoutControl = null
                };

                var http1Connection = new Http1Connection(http1ConnectionContext);

                http1Connection.Reset();

                IDictionary<string, StringValues> headers = http1Connection.ResponseHeaders;

                Assert.Equal(0, headers.Count);
                Assert.False(headers.IsReadOnly);
            }
        }

        [Theory]
        [InlineData("Server", "\r\nData")]
        [InlineData("Server", "\0Data")]
        [InlineData("Server", "Data\r")]
        [InlineData("Server", "Da\0ta")]
        [InlineData("Server", "Da\u001Fta")]
        [InlineData("Unknown-Header", "\r\nData")]
        [InlineData("Unknown-Header", "\0Data")]
        [InlineData("Unknown-Header", "Data\0")]
        [InlineData("Unknown-Header", "Da\nta")]
        [InlineData("\r\nServer", "Data")]
        [InlineData("Server\r", "Data")]
        [InlineData("Ser\0ver", "Data")]
        [InlineData("Server\r\n", "Data")]
        [InlineData("\u0000Server", "Data")]
        [InlineData("Server", "Data\u0000")]
        [InlineData("\u001FServer", "Data")]
        [InlineData("Unknown-Header\r\n", "Data")]
        [InlineData("\0Unknown-Header", "Data")]
        [InlineData("Unknown\r-Header", "Data")]
        [InlineData("Unk\nown-Header", "Data")]
        [InlineData("Server", "Da\u007Fta")]
        [InlineData("Unknown\u007F-Header", "Data")]
        [InlineData("Ser\u0080ver", "Data")]
        [InlineData("Server", "Da\u0080ta")]
        [InlineData("Unknown\u0080-Header", "Data")]
        [InlineData("Ser™ver", "Data")]
        [InlineData("Server", "Da™ta")]
        [InlineData("Unknown™-Header", "Data")]
        [InlineData("šerver", "Data")]
        [InlineData("Server", "Dašta")]
        [InlineData("Unknownš-Header", "Data")]
        [InlineData("Seršver", "Data")]
        [InlineData("Server\"", "Data")]
        [InlineData("Server(", "Data")]
        [InlineData("Server)", "Data")]
        [InlineData("Server,", "Data")]
        [InlineData("Server/", "Data")]
        [InlineData("Server:", "Data")]
        [InlineData("Server;", "Data")]
        [InlineData("Server<", "Data")]
        [InlineData("Server=", "Data")]
        [InlineData("Server>", "Data")]
        [InlineData("Server?", "Data")]
        [InlineData("Server@", "Data")]
        [InlineData("Server[", "Data")]
        [InlineData("Server\\", "Data")]
        [InlineData("Server]", "Data")]
        [InlineData("Server{", "Data")]
        [InlineData("Server}", "Data")]
        [InlineData("", "Data")]
        [InlineData(null, "Data")]
        public void AddingControlOrNonAsciiCharactersToHeadersThrows(string key, string value)
        {
            var responseHeaders = new HttpResponseHeaders();

            Assert.Throws<InvalidOperationException>(() =>
            {
                ((IHeaderDictionary)responseHeaders)[key] = value;
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                ((IHeaderDictionary)responseHeaders)[key] = new StringValues(new[] { "valid", value });
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                ((IDictionary<string, StringValues>)responseHeaders)[key] = value;
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                var kvp = new KeyValuePair<string, StringValues>(key, value);
                ((ICollection<KeyValuePair<string, StringValues>>)responseHeaders).Add(kvp);
            });

            Assert.Throws<InvalidOperationException>(() =>
            {
                var kvp = new KeyValuePair<string, StringValues>(key, value);
                ((IDictionary<string, StringValues>)responseHeaders).Add(key, value);
            });
        }

        [Fact]
        public void ThrowsWhenAddingHeaderAfterReadOnlyIsSet()
        {
            var headers = new HttpResponseHeaders();
            headers.SetReadOnly();

            Assert.Throws<InvalidOperationException>(() => ((IDictionary<string, StringValues>)headers).Add("my-header", new[] { "value" }));
        }

        [Fact]
        public void ThrowsWhenSettingContentLengthPropertyAfterReadOnlyIsSet()
        {
            var headers = new HttpResponseHeaders();
            headers.SetReadOnly();

            Assert.Throws<InvalidOperationException>(() => headers.ContentLength = null);
        }

        [Fact]
        public void ThrowsWhenChangingHeaderAfterReadOnlyIsSet()
        {
            var headers = new HttpResponseHeaders();
            var dictionary = (IDictionary<string, StringValues>)headers;
            dictionary.Add("my-header", new[] { "value" });
            headers.SetReadOnly();

            Assert.Throws<InvalidOperationException>(() => dictionary["my-header"] = "other-value");
        }

        [Fact]
        public void ThrowsWhenRemovingHeaderAfterReadOnlyIsSet()
        {
            var headers = new HttpResponseHeaders();
            var dictionary = (IDictionary<string, StringValues>)headers;
            dictionary.Add("my-header", new[] { "value" });
            headers.SetReadOnly();

            Assert.Throws<InvalidOperationException>(() => dictionary.Remove("my-header"));
        }

        [Fact]
        public void ThrowsWhenClearingHeadersAfterReadOnlyIsSet()
        {
            var headers = new HttpResponseHeaders();
            var dictionary = (IDictionary<string, StringValues>)headers;
            dictionary.Add("my-header", new[] { "value" });
            headers.SetReadOnly();

            Assert.Throws<InvalidOperationException>(() => dictionary.Clear());
        }

        [Theory]
        [MemberData(nameof(BadContentLengths))]
        public void ThrowsWhenAddingContentLengthWithNonNumericValue(string contentLength)
        {
            var headers = new HttpResponseHeaders();
            var dictionary = (IDictionary<string, StringValues>)headers;

            var exception = Assert.Throws<InvalidOperationException>(() => dictionary.Add("Content-Length", new[] { contentLength }));
            Assert.Equal(CoreStrings.FormatInvalidContentLength_InvalidNumber(contentLength), exception.Message);
        }

        [Theory]
        [MemberData(nameof(BadContentLengths))]
        public void ThrowsWhenSettingContentLengthToNonNumericValue(string contentLength)
        {
            var headers = new HttpResponseHeaders();
            var dictionary = (IDictionary<string, StringValues>)headers;

            var exception = Assert.Throws<InvalidOperationException>(() => ((IHeaderDictionary)headers)["Content-Length"] = contentLength);
            Assert.Equal(CoreStrings.FormatInvalidContentLength_InvalidNumber(contentLength), exception.Message);
        }

        [Theory]
        [MemberData(nameof(BadContentLengths))]
        public void ThrowsWhenAssigningHeaderContentLengthToNonNumericValue(string contentLength)
        {
            var headers = new HttpResponseHeaders();

            var exception = Assert.Throws<InvalidOperationException>(() => headers.HeaderContentLength = contentLength);
            Assert.Equal(CoreStrings.FormatInvalidContentLength_InvalidNumber(contentLength), exception.Message);
        }

        [Theory]
        [MemberData(nameof(GoodContentLengths))]
        public void ContentLengthValueCanBeReadAsLongAfterAddingHeader(string contentLength)
        {
            var headers = new HttpResponseHeaders();
            var dictionary = (IDictionary<string, StringValues>)headers;
            dictionary.Add("Content-Length", contentLength);

            Assert.Equal(ParseLong(contentLength), headers.ContentLength);
        }

        [Theory]
        [MemberData(nameof(GoodContentLengths))]
        public void ContentLengthValueCanBeReadAsLongAfterSettingHeader(string contentLength)
        {
            var headers = new HttpResponseHeaders();
            var dictionary = (IDictionary<string, StringValues>)headers;
            dictionary["Content-Length"] = contentLength;

            Assert.Equal(ParseLong(contentLength), headers.ContentLength);
        }

        [Theory]
        [MemberData(nameof(GoodContentLengths))]
        public void ContentLengthValueCanBeReadAsLongAfterAssigningHeader(string contentLength)
        {
            var headers = new HttpResponseHeaders();
            headers.HeaderContentLength = contentLength;

            Assert.Equal(ParseLong(contentLength), headers.ContentLength);
        }

        [Fact]
        public void ContentLengthValueClearedWhenHeaderIsRemoved()
        {
            var headers = new HttpResponseHeaders();
            headers.HeaderContentLength = "42";
            var dictionary = (IDictionary<string, StringValues>)headers;

            dictionary.Remove("Content-Length");

            Assert.Null(headers.ContentLength);
        }

        [Fact]
        public void ContentLengthValueClearedWhenHeadersCleared()
        {
            var headers = new HttpResponseHeaders();
            headers.HeaderContentLength = "42";
            var dictionary = (IDictionary<string, StringValues>)headers;

            dictionary.Clear();

            Assert.Null(headers.ContentLength);
        }

        private static long ParseLong(string value)
        {
            return long.Parse(value, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture);
        }

        public static TheoryData<string> GoodContentLengths => new TheoryData<string>
        {
            "0",
            "00",
            "042",
            "42",
            long.MaxValue.ToString(CultureInfo.InvariantCulture)
        };

        public static TheoryData<string> BadContentLengths => new TheoryData<string>
        {
            "",
            " ",
            " 42",
            "42 ",
            "bad",
            "!",
            "!42",
            "42!",
            "42,000",
            "42.000",
        };
    }
}
