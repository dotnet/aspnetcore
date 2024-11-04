// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Globalization;
using System.IO.Pipelines;
using System.Text;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class HttpResponseHeadersTests
{
    [Fact]
    public void InitialDictionaryIsEmpty()
    {
        using (var memoryPool = PinnedBlockMemoryPoolFactory.Create())
        {
            var options = new PipeOptions(memoryPool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);
            var pair = DuplexPipe.CreateConnectionPair(options, options);

            var connectionContext = Mock.Of<ConnectionContext>();
            var metricsContext = TestContextFactory.CreateMetricsContext(connectionContext);

            var connectionFeatures = new FeatureCollection();
            connectionFeatures.Set<IConnectionMetricsContextFeature>(new TestConnectionMetricsContextFeature { MetricsContext = metricsContext });

            var http1ConnectionContext = TestContextFactory.CreateHttpConnectionContext(
                serviceContext: new TestServiceContext(),
                connectionContext: connectionContext,
                transport: pair.Transport,
                memoryPool: memoryPool,
                connectionFeatures: connectionFeatures,
                metricsContext: metricsContext);

            var http1Connection = new Http1Connection(http1ConnectionContext);

            http1Connection.Reset();

            IDictionary<string, StringValues> headers = http1Connection.ResponseHeaders;

            Assert.Empty(headers);
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

    [Theory]
    [InlineData("\r\nData")]
    [InlineData("\0Data")]
    [InlineData("Data\r")]
    [InlineData("Da\0ta")]
    [InlineData("Da\u001Fta")]
    [InlineData("Data\0")]
    [InlineData("Da\nta")]
    [InlineData("Da\u007Fta")]
    [InlineData("Da\u0080ta")]
    [InlineData("Da™ta")]
    [InlineData("Dašta")]
    public void AddingControlOrNonAsciiCharactersToHeaderPropertyThrows(string value)
    {
        var responseHeaders = (IHeaderDictionary)new HttpResponseHeaders();

        // Known special header
        Assert.Throws<InvalidOperationException>(() =>
        {
            responseHeaders.Allow = value;
        });

        // Unknown header fallback
        Assert.Throws<InvalidOperationException>(() =>
        {
            responseHeaders.Accept = value;
        });
    }

    [Fact]
    public void AddingTabCharactersToHeaderPropertyWorks()
    {
        var responseHeaders = (IHeaderDictionary)new HttpResponseHeaders();

        // Known special header
        responseHeaders.Allow = "Da\tta";

        // Unknown header fallback
        responseHeaders.Accept = "Da\tta";
    }

    [Theory]
    [InlineData("\r\nData")]
    [InlineData("\0Data")]
    [InlineData("Data\r")]
    [InlineData("Da\0ta")]
    [InlineData("Da\u001Fta")]
    [InlineData("Data\0")]
    [InlineData("Da\nta")]
    [InlineData("Da\u007Fta")]
    public void AddingControlCharactersWithCustomEncoderThrows(string value)
    {
        var responseHeaders = new HttpResponseHeaders(_ => Encoding.UTF8);

        // Known special header
        Assert.Throws<InvalidOperationException>(() =>
        {
            ((IHeaderDictionary)responseHeaders).Allow = value;
        });

        // Unknown header fallback
        Assert.Throws<InvalidOperationException>(() =>
        {
            ((IHeaderDictionary)responseHeaders).Accept = value;
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            ((IHeaderDictionary)responseHeaders)["Unknown"] = value;
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            ((IHeaderDictionary)responseHeaders)["Unknown"] = new StringValues(new[] { "valid", value });
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            ((IDictionary<string, StringValues>)responseHeaders)["Unknown"] = value;
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            var kvp = new KeyValuePair<string, StringValues>("Unknown", value);
            ((ICollection<KeyValuePair<string, StringValues>>)responseHeaders).Add(kvp);
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            var kvp = new KeyValuePair<string, StringValues>("Unknown", value);
            ((IDictionary<string, StringValues>)responseHeaders).Add("Unknown", value);
        });
    }

    [Theory]
    [InlineData("Da\u0080ta")]
    [InlineData("Da™ta")]
    [InlineData("Dašta")]
    public void AddingNonAsciiCharactersWithCustomEncoderWorks(string value)
    {
        var responseHeaders = new HttpResponseHeaders(_ => Encoding.UTF8);

        // Known special header
        ((IHeaderDictionary)responseHeaders).Allow = value;

        // Unknown header fallback
        ((IHeaderDictionary)responseHeaders).Accept = value;

        ((IHeaderDictionary)responseHeaders)["Unknown"] = value;

        ((IHeaderDictionary)responseHeaders)["Unknown"] = new StringValues(new[] { "valid", value });

        ((IDictionary<string, StringValues>)responseHeaders)["Unknown"] = value;

        ((IHeaderDictionary)responseHeaders).Clear();
        var kvp = new KeyValuePair<string, StringValues>("Unknown", value);
        ((ICollection<KeyValuePair<string, StringValues>>)responseHeaders).Add(kvp);

        ((IHeaderDictionary)responseHeaders).Clear();
        kvp = new KeyValuePair<string, StringValues>("Unknown", value);
        ((IDictionary<string, StringValues>)responseHeaders).Add("Unknown", value);
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

    [Fact]
    public void ContentLengthEnumerableWithoutOtherKnownHeader()
    {
        IHeaderDictionary headers = new HttpResponseHeaders();
        headers["content-length"] = "1024";
        Assert.Single(headers);
        headers["unknown"] = "value";
        Assert.Equal(2, headers.Count()); // NB: enumerable count, not property
        headers["host"] = "myhost";
        Assert.Equal(3, headers.Count()); // NB: enumerable count, not property
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
