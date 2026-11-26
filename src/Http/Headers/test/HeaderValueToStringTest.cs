// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.Net.Http.Headers;

public class HeaderValueToStringTest
{
    [Fact]
    public void ToString_SequentialCalls_DoNotLeakPreviousContent()
    {
        var longCacheControl = new CacheControlHeaderValue { Private = true, MaxAge = TimeSpan.FromSeconds(60) };
        longCacheControl.PrivateHeaders.Add("X-Custom-Header-One");
        longCacheControl.PrivateHeaders.Add("X-Custom-Header-Two");
        longCacheControl.Extensions.Add(new NameValueHeaderValue("custom-extension", "\"some value\""));

        var mediaType = new MediaTypeHeaderValue("application/json") { Charset = "utf-8" };
        mediaType.Parameters.Add(new NameValueHeaderValue("q", "0.5"));

        for (var i = 0; i < 3; i++)
        {
            Assert.Equal("max-age=60, private=\"X-Custom-Header-One, X-Custom-Header-Two\", custom-extension=\"some value\"", longCacheControl.ToString());
            Assert.Equal("no-cache", new CacheControlHeaderValue { NoCache = true }.ToString());
            Assert.Equal("", new CacheControlHeaderValue().ToString());
            Assert.Equal("application/json; charset=utf-8; q=0.5", mediaType.ToString());
            Assert.Equal("text/plain", new MediaTypeHeaderValue("text/plain").ToString());
            Assert.Equal("bytes 0-1023/1048576", new ContentRangeHeaderValue(0, 1023, 1048576).ToString());
            Assert.Equal("bytes */100", new ContentRangeHeaderValue(100).ToString());
            Assert.Equal("bytes 1-2/*", new ContentRangeHeaderValue(1, 2).ToString());
            Assert.Equal("bytes=0-499, 500-, -100", new RangeHeaderValue { Ranges = { new(0, 499), new(500, null), new(null, 100) } }.ToString());
            Assert.Equal("inline", new ContentDispositionHeaderValue("inline").ToString());
        }
    }

    [Fact]
    public void ToString_LargeValues_FormattedCorrectly()
    {
        var name = new string('a', 500);
        var mediaType = new MediaTypeHeaderValue("text/plain");
        mediaType.Parameters.Add(new NameValueHeaderValue("p", name));

        var cacheControl = new CacheControlHeaderValue { NoStore = true };
        cacheControl.Extensions.Add(new NameValueHeaderValue(name));

        for (var i = 0; i < 2; i++)
        {
            Assert.Equal("text/plain; p=" + name, mediaType.ToString());
            Assert.Equal("no-store, " + name, cacheControl.ToString());
            Assert.Equal("text/html", new MediaTypeHeaderValue("text/html").ToString());
        }
    }

    [Fact]
    public void ToString_ContentDisposition_MatchesExpected()
    {
        var contentDisposition = new ContentDispositionHeaderValue("attachment");
        contentDisposition.SetHttpFileName("report-2024.pdf");

        Assert.Equal("attachment; filename=report-2024.pdf; filename*=UTF-8''report-2024.pdf", contentDisposition.ToString());
    }

    [Fact]
    public void ToString_NumbersUseInvariantCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.NegativeSign = "~";
            CultureInfo.CurrentCulture = culture;

            Assert.Equal("max-age=-5, s-maxage=10", new CacheControlHeaderValue { MaxAge = TimeSpan.FromSeconds(-5), SharedMaxAge = TimeSpan.FromSeconds(10) }.ToString());
            Assert.Equal("max-stale=-1, min-fresh=-2", new CacheControlHeaderValue { MaxStale = true, MaxStaleLimit = TimeSpan.FromSeconds(-1), MinFresh = TimeSpan.FromSeconds(-2) }.ToString());
            Assert.Equal("bytes 9223372036854775805-9223372036854775806/9223372036854775807", new ContentRangeHeaderValue(long.MaxValue - 2, long.MaxValue - 1, long.MaxValue).ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
