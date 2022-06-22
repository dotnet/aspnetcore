// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.OutputCaching.Memory;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.OutputCaching.Tests;

public class OutputCacheMiddlewareTests
{
    [Fact]
    public async Task TryServeFromCacheAsync_OnlyIfCached_Serves504()
    {
        var cache = new TestOutputCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache, keyProvider: new TestResponseCachingKeyProvider("BaseKey"));
        var context = TestUtils.CreateTestContext(cache);
        context.HttpContext.Request.Headers.CacheControl = new CacheControlHeaderValue()
        {
            OnlyIfCached = true
        }.ToString();
        middleware.TryGetRequestPolicies(context.HttpContext, out var policies);

        Assert.True(await middleware.TryServeFromCacheAsync(context, policies));
        Assert.Equal(StatusCodes.Status504GatewayTimeout, context.HttpContext.Response.StatusCode);
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.GatewayTimeoutServed);
    }

    [Fact]
    public async Task TryServeFromCacheAsync_CachedResponseNotFound_Fails()
    {
        var cache = new TestOutputCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache, keyProvider: new TestResponseCachingKeyProvider("BaseKey"));
        var context = TestUtils.CreateTestContext(cache);
        middleware.TryGetRequestPolicies(context.HttpContext, out var policies);

        Assert.False(await middleware.TryServeFromCacheAsync(context, policies));
        Assert.Equal(1, cache.GetCount);
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.NoResponseServed);
    }

    [Fact]
    public async Task TryServeFromCacheAsync_CachedResponseFound_Succeeds()
    {
        var cache = new TestOutputCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache, keyProvider: new TestResponseCachingKeyProvider("BaseKey"));
        var context = TestUtils.CreateTestContext(cache);
        middleware.TryGetRequestPolicies(context.HttpContext, out var policies);

        await OutputCacheEntryFormatter.StoreAsync(
            "BaseKey",
            new OutputCacheEntry()
            {
                Headers = new HeaderDictionary(),
                Body = new CachedResponseBody(new List<byte[]>(0), 0)
            },
            TimeSpan.Zero,
            cache,
            default);

        Assert.True(await middleware.TryServeFromCacheAsync(context, policies));
        Assert.Equal(1, cache.GetCount);
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.CachedResponseServed);
    }

    [Fact]
    public async Task TryServeFromCacheAsync_CachedResponseFound_OverwritesExistingHeaders()
    {
        var cache = new TestOutputCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache, keyProvider: new TestResponseCachingKeyProvider("BaseKey"));
        var context = TestUtils.CreateTestContext(cache);
        middleware.TryGetRequestPolicies(context.HttpContext, out var policies);
        context.CacheKey = "BaseKey";

        context.HttpContext.Response.Headers["MyHeader"] = "OldValue";
        await OutputCacheEntryFormatter.StoreAsync(context.CacheKey,
            new OutputCacheEntry()
            {
                Headers = new HeaderDictionary()
                {
                        { "MyHeader", "NewValue" }
                },
                Body = new CachedResponseBody(new List<byte[]>(0), 0)
            },
            TimeSpan.Zero,
            cache,
            default);

        Assert.True(await middleware.TryServeFromCacheAsync(context, policies));
        Assert.Equal("NewValue", context.HttpContext.Response.Headers["MyHeader"]);
        Assert.Equal(1, cache.GetCount);
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.CachedResponseServed);
    }

    [Fact]
    public async Task TryServeFromCacheAsync_CachedResponseFound_Serves304IfPossible()
    {
        var cache = new TestOutputCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache, keyProvider: new TestResponseCachingKeyProvider("BaseKey"));
        var context = TestUtils.CreateTestContext(cache);
        context.HttpContext.Request.Headers.IfNoneMatch = "*";
        middleware.TryGetRequestPolicies(context.HttpContext, out var policies);

        await OutputCacheEntryFormatter.StoreAsync("BaseKey",
            new OutputCacheEntry()
            {
                Body = new CachedResponseBody(new List<byte[]>(0), 0),
                Headers = new()
            },
            TimeSpan.Zero,
            cache,
            default);

        Assert.True(await middleware.TryServeFromCacheAsync(context, policies));
        Assert.Equal(1, cache.GetCount);
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.NotModifiedServed);
    }

    [Fact]
    public void ContentIsNotModified_NotConditionalRequest_False()
    {
        var sink = new TestSink();
        var context = TestUtils.CreateTestContext(sink);
        context.CachedResponse = new OutputCacheEntry { Headers = new HeaderDictionary() };

        Assert.False(OutputCacheMiddleware.ContentIsNotModified(context));
        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void ContentIsNotModified_IfModifiedSince_FallsbackToDateHeader()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var sink = new TestSink();
        var context = TestUtils.CreateTestContext(sink);
        context.CachedResponse = new OutputCacheEntry { Headers = new HeaderDictionary() };

        context.HttpContext.Request.Headers.IfModifiedSince = HeaderUtilities.FormatDate(utcNow);

        // Verify modifications in the past succeeds
        context.CachedResponse.Headers[HeaderNames.Date] = HeaderUtilities.FormatDate(utcNow - TimeSpan.FromSeconds(10));
        Assert.True(OutputCacheMiddleware.ContentIsNotModified(context));
        Assert.Single(sink.Writes);

        // Verify modifications at present succeeds
        context.CachedResponse.Headers[HeaderNames.Date] = HeaderUtilities.FormatDate(utcNow);
        Assert.True(OutputCacheMiddleware.ContentIsNotModified(context));
        Assert.Equal(2, sink.Writes.Count);

        // Verify modifications in the future fails
        context.CachedResponse.Headers[HeaderNames.Date] = HeaderUtilities.FormatDate(utcNow + TimeSpan.FromSeconds(10));
        Assert.False(OutputCacheMiddleware.ContentIsNotModified(context));

        // Verify logging
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.NotModifiedIfModifiedSinceSatisfied,
            LoggedMessage.NotModifiedIfModifiedSinceSatisfied);
    }

    [Fact]
    public void ContentIsNotModified_IfModifiedSince_LastModifiedOverridesDateHeader()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var sink = new TestSink();
        var context = TestUtils.CreateTestContext(sink);
        context.CachedResponse = new OutputCacheEntry { Headers = new HeaderDictionary() };

        context.HttpContext.Request.Headers.IfModifiedSince = HeaderUtilities.FormatDate(utcNow);

        // Verify modifications in the past succeeds
        context.CachedResponse.Headers[HeaderNames.Date] = HeaderUtilities.FormatDate(utcNow + TimeSpan.FromSeconds(10));
        context.CachedResponse.Headers[HeaderNames.LastModified] = HeaderUtilities.FormatDate(utcNow - TimeSpan.FromSeconds(10));
        Assert.True(OutputCacheMiddleware.ContentIsNotModified(context));
        Assert.Single(sink.Writes);

        // Verify modifications at present
        context.CachedResponse.Headers[HeaderNames.Date] = HeaderUtilities.FormatDate(utcNow + TimeSpan.FromSeconds(10));
        context.CachedResponse.Headers[HeaderNames.LastModified] = HeaderUtilities.FormatDate(utcNow);
        Assert.True(OutputCacheMiddleware.ContentIsNotModified(context));
        Assert.Equal(2, sink.Writes.Count);

        // Verify modifications in the future fails
        context.CachedResponse.Headers[HeaderNames.Date] = HeaderUtilities.FormatDate(utcNow - TimeSpan.FromSeconds(10));
        context.CachedResponse.Headers[HeaderNames.LastModified] = HeaderUtilities.FormatDate(utcNow + TimeSpan.FromSeconds(10));
        Assert.False(OutputCacheMiddleware.ContentIsNotModified(context));

        // Verify logging
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.NotModifiedIfModifiedSinceSatisfied,
            LoggedMessage.NotModifiedIfModifiedSinceSatisfied);
    }

    [Fact]
    public void ContentIsNotModified_IfNoneMatch_Overrides_IfModifiedSince_ToTrue()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var sink = new TestSink();
        var context = TestUtils.CreateTestContext(sink);
        context.CachedResponse = new OutputCacheEntry { Headers = new HeaderDictionary() };

        // This would fail the IfModifiedSince checks
        context.HttpContext.Request.Headers.IfModifiedSince = HeaderUtilities.FormatDate(utcNow);
        context.CachedResponse.Headers[HeaderNames.LastModified] = HeaderUtilities.FormatDate(utcNow + TimeSpan.FromSeconds(10));

        context.HttpContext.Request.Headers.IfNoneMatch = EntityTagHeaderValue.Any.ToString();
        Assert.True(OutputCacheMiddleware.ContentIsNotModified(context));
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.NotModifiedIfNoneMatchStar);
    }

    [Fact]
    public void ContentIsNotModified_IfNoneMatch_Overrides_IfModifiedSince_ToFalse()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var sink = new TestSink();
        var context = TestUtils.CreateTestContext(sink);
        context.CachedResponse = new OutputCacheEntry { Headers = new HeaderDictionary() };

        // This would pass the IfModifiedSince checks
        context.HttpContext.Request.Headers.IfModifiedSince = HeaderUtilities.FormatDate(utcNow);
        context.CachedResponse.Headers[HeaderNames.LastModified] = HeaderUtilities.FormatDate(utcNow - TimeSpan.FromSeconds(10));

        context.HttpContext.Request.Headers.IfNoneMatch = "\"E1\"";
        Assert.False(OutputCacheMiddleware.ContentIsNotModified(context));
        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void ContentIsNotModified_IfNoneMatch_AnyWithoutETagInResponse_False()
    {
        var sink = new TestSink();
        var context = TestUtils.CreateTestContext(sink);
        context.CachedResponse = new OutputCacheEntry { Headers = new HeaderDictionary() };
        context.HttpContext.Request.Headers.IfNoneMatch = "\"E1\"";

        Assert.False(OutputCacheMiddleware.ContentIsNotModified(context));
        Assert.Empty(sink.Writes);
    }

    public static TheoryData<EntityTagHeaderValue, EntityTagHeaderValue> EquivalentWeakETags
    {
        get
        {
            return new TheoryData<EntityTagHeaderValue, EntityTagHeaderValue>
                {
                    { new EntityTagHeaderValue("\"tag\""), new EntityTagHeaderValue("\"tag\"") },
                    { new EntityTagHeaderValue("\"tag\"", true), new EntityTagHeaderValue("\"tag\"") },
                    { new EntityTagHeaderValue("\"tag\""), new EntityTagHeaderValue("\"tag\"", true) },
                    { new EntityTagHeaderValue("\"tag\"", true), new EntityTagHeaderValue("\"tag\"", true) }
                };
        }
    }

    [Theory]
    [MemberData(nameof(EquivalentWeakETags))]
    public void ContentIsNotModified_IfNoneMatch_ExplicitWithMatch_True(EntityTagHeaderValue responseETag, EntityTagHeaderValue requestETag)
    {
        var sink = new TestSink();
        var context = TestUtils.CreateTestContext(sink);
        context.CachedResponse = new OutputCacheEntry { Headers = new HeaderDictionary() };
        context.CachedResponse.Headers[HeaderNames.ETag] = responseETag.ToString();
        context.HttpContext.Request.Headers.IfNoneMatch = requestETag.ToString();

        Assert.True(OutputCacheMiddleware.ContentIsNotModified(context));
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.NotModifiedIfNoneMatchMatched);
    }

    [Fact]
    public void ContentIsNotModified_IfNoneMatch_ExplicitWithoutMatch_False()
    {
        var sink = new TestSink();
        var context = TestUtils.CreateTestContext(sink);
        context.CachedResponse = new OutputCacheEntry { Headers = new HeaderDictionary() };
        context.CachedResponse.Headers[HeaderNames.ETag] = "\"E2\"";
        context.HttpContext.Request.Headers.IfNoneMatch = "\"E1\"";

        Assert.False(OutputCacheMiddleware.ContentIsNotModified(context));
        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void ContentIsNotModified_IfNoneMatch_MatchesAtLeastOneValue_True()
    {
        var sink = new TestSink();
        var context = TestUtils.CreateTestContext(sink);
        context.CachedResponse = new OutputCacheEntry { Headers = new HeaderDictionary() };
        context.CachedResponse.Headers[HeaderNames.ETag] = "\"E2\"";
        context.HttpContext.Request.Headers.IfNoneMatch = new string[] { "\"E0\", \"E1\"", "\"E1\", \"E2\"" };

        Assert.True(OutputCacheMiddleware.ContentIsNotModified(context));
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.NotModifiedIfNoneMatchMatched);
    }

    [Fact]
    public void StartResponsegAsync_IfAllowResponseCaptureIsTrue_SetsResponseTime()
    {
        var clock = new TestClock
        {
            UtcNow = DateTimeOffset.UtcNow
        };
        var middleware = TestUtils.CreateTestMiddleware(options: new OutputCacheOptions
        {
            SystemClock = clock
        });
        var context = TestUtils.CreateTestContext();
        context.ResponseTime = null;

        middleware.StartResponse(context);

        Assert.Equal(clock.UtcNow, context.ResponseTime);
    }

    [Fact]
    public void StartResponseAsync_IfAllowResponseCaptureIsTrue_SetsResponseTimeOnlyOnce()
    {
        var clock = new TestClock
        {
            UtcNow = DateTimeOffset.UtcNow
        };
        var middleware = TestUtils.CreateTestMiddleware(options: new OutputCacheOptions
        {
            SystemClock = clock
        });
        var context = TestUtils.CreateTestContext();
        var initialTime = clock.UtcNow;
        context.ResponseTime = null;

        middleware.StartResponse(context);
        Assert.Equal(initialTime, context.ResponseTime);

        clock.UtcNow += TimeSpan.FromSeconds(10);

        middleware.StartResponse(context);
        Assert.NotEqual(clock.UtcNow, context.ResponseTime);
        Assert.Equal(initialTime, context.ResponseTime);
    }

    [Fact]
    public void FinalizeCacheHeadersAsync_DefaultResponseValidity_Is60Seconds()
    {
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink);
        var context = TestUtils.CreateTestContext();

        middleware.FinalizeCacheHeaders(context);

        Assert.Equal(TimeSpan.FromSeconds(60), context.CachedResponseValidFor);
        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void FinalizeCacheHeadersAsync_ResponseValidity_IgnoresExpiryIfAvailable()
    {
        // The Expires header should not be used when set in the response

        var clock = new TestClock
        {
            UtcNow = DateTimeOffset.MinValue
        };
        var options = new OutputCacheOptions
        {
            SystemClock = clock
        };
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, options: options);
        var context = TestUtils.CreateTestContext();

        context.ResponseTime = clock.UtcNow;
        context.HttpContext.Response.Headers.Expires = HeaderUtilities.FormatDate(clock.UtcNow + TimeSpan.FromSeconds(11));

        middleware.FinalizeCacheHeaders(context);

        Assert.Equal(options.DefaultExpirationTimeSpan, context.CachedResponseValidFor);
        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void FinalizeCacheHeadersAsync_ResponseValidity_UseMaxAgeIfAvailable()
    {
        // The MaxAge header should not be used if set in the response

        var clock = new TestClock
        {
            UtcNow = DateTimeOffset.UtcNow
        };
        var sink = new TestSink();
        var options = new OutputCacheOptions
        {
            SystemClock = clock
        };
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, options: options);
        var context = TestUtils.CreateTestContext();

        context.ResponseTime = clock.UtcNow;
        context.HttpContext.Response.Headers.CacheControl = new CacheControlHeaderValue()
        {
            MaxAge = TimeSpan.FromSeconds(12)
        }.ToString();

        context.HttpContext.Response.Headers.Expires = HeaderUtilities.FormatDate(clock.UtcNow + TimeSpan.FromSeconds(11));

        middleware.FinalizeCacheHeaders(context);

        Assert.Equal(options.DefaultExpirationTimeSpan, context.CachedResponseValidFor);
        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void FinalizeCacheHeadersAsync_ResponseValidity_UseSharedMaxAgeIfAvailable()
    {
        var clock = new TestClock
        {
            UtcNow = DateTimeOffset.UtcNow
        };
        var sink = new TestSink();
        var options = new OutputCacheOptions
        {
            SystemClock = clock
        };
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, options: options);
        var context = TestUtils.CreateTestContext();

        context.ResponseTime = clock.UtcNow;
        context.HttpContext.Response.Headers.CacheControl = new CacheControlHeaderValue()
        {
            MaxAge = TimeSpan.FromSeconds(12),
            SharedMaxAge = TimeSpan.FromSeconds(13)
        }.ToString();
        context.HttpContext.Response.Headers.Expires = HeaderUtilities.FormatDate(clock.UtcNow + TimeSpan.FromSeconds(11));

        middleware.FinalizeCacheHeaders(context);

        Assert.Equal(options.DefaultExpirationTimeSpan, context.CachedResponseValidFor);
        Assert.Empty(sink.Writes);
    }

    public static TheoryData<StringValues> NullOrEmptyVaryRules
    {
        get
        {
            return new TheoryData<StringValues>
                {
                    default(StringValues),
                    StringValues.Empty,
                    new StringValues((string)null),
                    new StringValues(string.Empty),
                    new StringValues((string[])null),
                    new StringValues(new string[0]),
                    new StringValues(new string[] { null }),
                    new StringValues(new string[] { string.Empty })
                };
        }
    }

    [Theory]
    [MemberData(nameof(NullOrEmptyVaryRules))]
    public void FinalizeCacheHeadersAsync_UpdateCachedVaryByRules_NullOrEmptyRules(StringValues vary)
    {
        var cache = new TestOutputCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache);
        var context = TestUtils.CreateTestContext(cache);

        context.HttpContext.Response.Headers.Vary = vary;
        context.HttpContext.Features.Set<IOutputCacheFeature>(new OutputCacheFeature(context));
        context.CacheVaryByRules.QueryKeys = vary;

        middleware.FinalizeCacheHeaders(context);

        // Vary rules should not be updated
        Assert.Equal(0, cache.SetCount);
        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void FinalizeCacheHeadersAsync_AddsDate_IfNoneSpecified()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink);
        var context = TestUtils.CreateTestContext();
        // ResponseTime is the actual value that's used to set the Date header in FinalizeCacheHeadersAsync
        context.ResponseTime = utcNow;

        Assert.True(StringValues.IsNullOrEmpty(context.HttpContext.Response.Headers.Date));

        middleware.FinalizeCacheHeaders(context);

        Assert.Equal(HeaderUtilities.FormatDate(utcNow), context.HttpContext.Response.Headers.Date);
        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void FinalizeCacheHeadersAsync_IgnoresDate_IfSpecified()
    {
        // The Date header should not be used when set in the response

        var utcNow = DateTimeOffset.UtcNow;
        var responseTime = utcNow + TimeSpan.FromSeconds(10);
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink);
        var context = TestUtils.CreateTestContext();

        context.HttpContext.Response.Headers.Date = HeaderUtilities.FormatDate(utcNow);
        context.ResponseTime = responseTime;

        Assert.Equal(HeaderUtilities.FormatDate(utcNow), context.HttpContext.Response.Headers.Date);

        middleware.FinalizeCacheHeaders(context);

        Assert.Equal(HeaderUtilities.FormatDate(responseTime), context.HttpContext.Response.Headers.Date);
        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void FinalizeCacheHeadersAsync_StoresCachedResponse_InState()
    {
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink);
        var context = TestUtils.CreateTestContext();

        Assert.Null(context.CachedResponse);

        middleware.FinalizeCacheHeaders(context);

        Assert.NotNull(context.CachedResponse);
        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void FinalizeCacheHeadersAsync_StoresHeaders()
    {
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink);
        var context = TestUtils.CreateTestContext();

        context.HttpContext.Response.Headers.Vary = "HeaderB, heaDera";

        middleware.FinalizeCacheHeaders(context);

        Assert.Equal(new StringValues(new[] { "HeaderB, heaDera" }), context.CachedResponse.Headers[HeaderNames.Vary]);
    }

    [Fact]
    public async Task FinalizeCacheBody_Cache_IfContentLengthMatches()
    {
        var cache = new TestOutputCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache);
        var context = TestUtils.CreateTestContext(cache);

        middleware.ShimResponseStream(context);
        context.HttpContext.Response.ContentLength = 20;

        await context.HttpContext.Response.WriteAsync(new string('0', 20));

        context.CachedResponse = new OutputCacheEntry { Headers = new() };
        context.CacheKey = "BaseKey";
        context.CachedResponseValidFor = TimeSpan.FromSeconds(10);

        await middleware.FinalizeCacheBodyAsync(context);

        Assert.Equal(1, cache.SetCount);
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.ResponseCached);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    public async Task FinalizeCacheBody_DoNotCache_IfContentLengthMismatches(string method)
    {
        var cache = new TestOutputCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache);
        var context = TestUtils.CreateTestContext(cache);

        middleware.ShimResponseStream(context);
        context.HttpContext.Response.ContentLength = 9;
        context.HttpContext.Request.Method = method;

        await context.HttpContext.Response.WriteAsync(new string('0', 10));

        context.CachedResponse = new OutputCacheEntry();
        context.CacheKey = "BaseKey";
        context.CachedResponseValidFor = TimeSpan.FromSeconds(10);

        await middleware.FinalizeCacheBodyAsync(context);

        Assert.Equal(0, cache.SetCount);
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.ResponseContentLengthMismatchNotCached);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FinalizeCacheBody_RequestHead_Cache_IfContentLengthPresent_AndBodyAbsentOrOfSameLength(bool includeBody)
    {
        var cache = new TestOutputCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache);
        var context = TestUtils.CreateTestContext(cache);

        middleware.ShimResponseStream(context);
        context.HttpContext.Response.ContentLength = 10;
        context.HttpContext.Request.Method = "HEAD";

        if (includeBody)
        {
            // A response to HEAD should not include a body, but it may be present
            await context.HttpContext.Response.WriteAsync(new string('0', 10));
        }

        context.CachedResponse = new OutputCacheEntry { Headers = new() };
        context.CacheKey = "BaseKey";
        context.CachedResponseValidFor = TimeSpan.FromSeconds(10);

        await middleware.FinalizeCacheBodyAsync(context);

        Assert.Equal(1, cache.SetCount);
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.ResponseCached);
    }

    [Fact]
    public async Task FinalizeCacheBody_Cache_IfContentLengthAbsent()
    {
        var cache = new TestOutputCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache);
        var context = TestUtils.CreateTestContext(cache);

        middleware.ShimResponseStream(context);

        await context.HttpContext.Response.WriteAsync(new string('0', 10));

        context.CachedResponse = new OutputCacheEntry { Headers = new HeaderDictionary() };
        context.CacheKey = "BaseKey";
        context.CachedResponseValidFor = TimeSpan.FromSeconds(10);

        await middleware.FinalizeCacheBodyAsync(context);

        Assert.Equal(1, cache.SetCount);
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.ResponseCached);
    }

    [Fact]
    public async Task FinalizeCacheBody_DoNotCache_IfIsResponseCacheableFalse()
    {
        var cache = new TestOutputCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache);
        var context = TestUtils.CreateTestContext(cache);

        middleware.ShimResponseStream(context);
        await context.HttpContext.Response.WriteAsync(new string('0', 10));
        context.AllowCacheStorage = false;
        context.CacheKey = "BaseKey";

        await middleware.FinalizeCacheBodyAsync(context);

        Assert.Equal(0, cache.SetCount);
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.ResponseNotCached);
    }

    [Fact]
    public async Task FinalizeCacheBody_DoNotCache_IfBufferingDisabled()
    {
        var cache = new TestOutputCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache);
        var context = TestUtils.CreateTestContext(cache);

        middleware.ShimResponseStream(context);
        await context.HttpContext.Response.WriteAsync(new string('0', 10));

        context.OutputCacheStream.DisableBuffering();

        await middleware.FinalizeCacheBodyAsync(context);

        Assert.Equal(0, cache.SetCount);
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.ResponseNotCached);
    }

    [Fact]
    public async Task FinalizeCacheBody_DoNotCache_IfSizeTooBig()
    {
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(
            testSink: sink,
            keyProvider: new TestResponseCachingKeyProvider("BaseKey"),
            cache: new MemoryOutputCacheStore(new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 100
            })));
        var context = TestUtils.CreateTestContext();
        middleware.TryGetRequestPolicies(context.HttpContext, out var policies);
        middleware.ShimResponseStream(context);

        await context.HttpContext.Response.WriteAsync(new string('0', 101));

        context.CachedResponse = new OutputCacheEntry() { Headers = new HeaderDictionary() };
        context.CacheKey = "BaseKey";
        context.CachedResponseValidFor = TimeSpan.FromSeconds(10);

        await middleware.FinalizeCacheBodyAsync(context);

        // The response cached message will be logged but the adding of the entry will no-op
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.ResponseCached);

        // The entry cannot be retrieved
        Assert.False(await middleware.TryServeFromCacheAsync(context, policies));
    }

    [Fact]
    public void AddOutputCachingFeature_SecondInvocation_Throws()
    {
        var httpContext = new DefaultHttpContext();
        var context = TestUtils.CreateTestContext(httpContext);

        // Should not throw
        OutputCacheMiddleware.AddOutputCacheFeature(context);

        // Should throw
        Assert.ThrowsAny<InvalidOperationException>(() => OutputCacheMiddleware.AddOutputCacheFeature(context));
    }

    private class FakeResponseFeature : HttpResponseFeature
    {
        public override void OnStarting(Func<object, Task> callback, object state) { }
    }

    [Fact]
    public void GetOrderCasingNormalizedStringValues_NormalizesCasingToUpper()
    {
        var uppercaseStrings = new StringValues(new[] { "STRINGA", "STRINGB" });
        var lowercaseStrings = new StringValues(new[] { "stringA", "stringB" });

        var normalizedStrings = OutputCacheMiddleware.GetOrderCasingNormalizedStringValues(lowercaseStrings);

        Assert.Equal(uppercaseStrings, normalizedStrings);
    }

    [Fact]
    public void GetOrderCasingNormalizedStringValues_NormalizesOrder()
    {
        var orderedStrings = new StringValues(new[] { "STRINGA", "STRINGB" });
        var reverseOrderStrings = new StringValues(new[] { "STRINGB", "STRINGA" });

        var normalizedStrings = OutputCacheMiddleware.GetOrderCasingNormalizedStringValues(reverseOrderStrings);

        Assert.Equal(orderedStrings, normalizedStrings);
    }

    [Fact]
    public void GetOrderCasingNormalizedStringValues_PreservesCommas()
    {
        var originalStrings = new StringValues(new[] { "STRINGA, STRINGB" });

        var normalizedStrings = OutputCacheMiddleware.GetOrderCasingNormalizedStringValues(originalStrings);

        Assert.Equal(originalStrings, normalizedStrings);
    }
}
