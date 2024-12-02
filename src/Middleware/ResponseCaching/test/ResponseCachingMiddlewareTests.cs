// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.ResponseCaching.Tests;

public class ResponseCachingMiddlewareTests
{
    [Fact]
    public async Task TryServeFromCacheAsync_OnlyIfCached_Serves504()
    {
        var cache = new TestResponseCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache, keyProvider: new TestResponseCachingKeyProvider());
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.Headers.CacheControl = new CacheControlHeaderValue()
        {
            OnlyIfCached = true
        }.ToString();

        Assert.True(await middleware.TryServeFromCacheAsync(context));
        Assert.Equal(StatusCodes.Status504GatewayTimeout, context.HttpContext.Response.StatusCode);
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.GatewayTimeoutServed);
    }

    [Fact]
    public async Task TryServeFromCacheAsync_CachedResponseNotFound_Fails()
    {
        var cache = new TestResponseCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache, keyProvider: new TestResponseCachingKeyProvider("BaseKey"));
        var context = TestUtils.CreateTestContext();

        Assert.False(await middleware.TryServeFromCacheAsync(context));
        Assert.Equal(1, cache.GetCount);
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.NoResponseServed);
    }

    [Fact]
    public async Task TryServeFromCacheAsync_CachedResponseFound_Succeeds()
    {
        var cache = new TestResponseCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache, keyProvider: new TestResponseCachingKeyProvider("BaseKey"));
        var context = TestUtils.CreateTestContext();

        cache.Set(
            "BaseKey",
            new CachedResponse()
            {
                Headers = new HeaderDictionary(),
                Body = new CachedResponseBody(new List<byte[]>(0), 0)
            },
            TimeSpan.Zero);

        Assert.True(await middleware.TryServeFromCacheAsync(context));
        Assert.Equal(1, cache.GetCount);
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.CachedResponseServed);
    }

    [Fact]
    public async Task TryServeFromCacheAsync_CachedResponseFound_OverwritesExistingHeaders()
    {
        var cache = new TestResponseCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache, keyProvider: new TestResponseCachingKeyProvider("BaseKey"));
        var context = TestUtils.CreateTestContext();

        context.HttpContext.Response.Headers["MyHeader"] = "OldValue";
        cache.Set(
            "BaseKey",
            new CachedResponse()
            {
                Headers = new HeaderDictionary()
                {
                        { "MyHeader", "NewValue" }
                },
                Body = new CachedResponseBody(new List<byte[]>(0), 0)
            },
            TimeSpan.Zero);

        Assert.True(await middleware.TryServeFromCacheAsync(context));
        Assert.Equal("NewValue", context.HttpContext.Response.Headers["MyHeader"]);
        Assert.Equal(1, cache.GetCount);
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.CachedResponseServed);
    }

    [Fact]
    public async Task TryServeFromCacheAsync_VaryByRuleFound_CachedResponseNotFound_Fails()
    {
        var cache = new TestResponseCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache, keyProvider: new TestResponseCachingKeyProvider("BaseKey", "VaryKey"));
        var context = TestUtils.CreateTestContext();

        cache.Set(
            "BaseKey",
            new CachedVaryByRules(),
            TimeSpan.Zero);

        Assert.False(await middleware.TryServeFromCacheAsync(context));
        Assert.Equal(2, cache.GetCount);
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.NoResponseServed);
    }

    [Fact]
    public async Task TryServeFromCacheAsync_VaryByRuleFound_CachedResponseFound_Succeeds()
    {
        var cache = new TestResponseCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache, keyProvider: new TestResponseCachingKeyProvider("BaseKey", new[] { "VaryKey", "VaryKey2" }));
        var context = TestUtils.CreateTestContext();

        cache.Set(
            "BaseKey",
            new CachedVaryByRules(),
            TimeSpan.Zero);
        cache.Set(
            "BaseKeyVaryKey2",
            new CachedResponse()
            {
                Headers = new HeaderDictionary(),
                Body = new CachedResponseBody(new List<byte[]>(0), 0)
            },
            TimeSpan.Zero);

        Assert.True(await middleware.TryServeFromCacheAsync(context));
        Assert.Equal(3, cache.GetCount);
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.CachedResponseServed);
    }

    [Fact]
    public async Task TryServeFromCacheAsync_CachedResponseFound_Serves304IfPossible()
    {
        var cache = new TestResponseCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache, keyProvider: new TestResponseCachingKeyProvider("BaseKey"));
        var context = TestUtils.CreateTestContext();
        context.HttpContext.Request.Headers.IfNoneMatch = "*";

        cache.Set(
            "BaseKey",
            new CachedResponse()
            {
                Body = new CachedResponseBody(new List<byte[]>(0), 0)
            },
            TimeSpan.Zero);

        Assert.True(await middleware.TryServeFromCacheAsync(context));
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
        context.CachedResponseHeaders = new HeaderDictionary();

        Assert.False(ResponseCachingMiddleware.ContentIsNotModified(context));
        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void ContentIsNotModified_IfModifiedSince_FallsbackToDateHeader()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var sink = new TestSink();
        var context = TestUtils.CreateTestContext(sink);
        context.CachedResponseHeaders = new HeaderDictionary();

        context.HttpContext.Request.Headers.IfModifiedSince = HeaderUtilities.FormatDate(utcNow);

        // Verify modifications in the past succeeds
        context.CachedResponseHeaders[HeaderNames.Date] = HeaderUtilities.FormatDate(utcNow - TimeSpan.FromSeconds(10));
        Assert.True(ResponseCachingMiddleware.ContentIsNotModified(context));
        Assert.Single(sink.Writes);

        // Verify modifications at present succeeds
        context.CachedResponseHeaders[HeaderNames.Date] = HeaderUtilities.FormatDate(utcNow);
        Assert.True(ResponseCachingMiddleware.ContentIsNotModified(context));
        Assert.Equal(2, sink.Writes.Count);

        // Verify modifications in the future fails
        context.CachedResponseHeaders[HeaderNames.Date] = HeaderUtilities.FormatDate(utcNow + TimeSpan.FromSeconds(10));
        Assert.False(ResponseCachingMiddleware.ContentIsNotModified(context));

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
        context.CachedResponseHeaders = new HeaderDictionary();

        context.HttpContext.Request.Headers.IfModifiedSince = HeaderUtilities.FormatDate(utcNow);

        // Verify modifications in the past succeeds
        context.CachedResponseHeaders[HeaderNames.Date] = HeaderUtilities.FormatDate(utcNow + TimeSpan.FromSeconds(10));
        context.CachedResponseHeaders[HeaderNames.LastModified] = HeaderUtilities.FormatDate(utcNow - TimeSpan.FromSeconds(10));
        Assert.True(ResponseCachingMiddleware.ContentIsNotModified(context));
        Assert.Single(sink.Writes);

        // Verify modifications at present
        context.CachedResponseHeaders[HeaderNames.Date] = HeaderUtilities.FormatDate(utcNow + TimeSpan.FromSeconds(10));
        context.CachedResponseHeaders[HeaderNames.LastModified] = HeaderUtilities.FormatDate(utcNow);
        Assert.True(ResponseCachingMiddleware.ContentIsNotModified(context));
        Assert.Equal(2, sink.Writes.Count);

        // Verify modifications in the future fails
        context.CachedResponseHeaders[HeaderNames.Date] = HeaderUtilities.FormatDate(utcNow - TimeSpan.FromSeconds(10));
        context.CachedResponseHeaders[HeaderNames.LastModified] = HeaderUtilities.FormatDate(utcNow + TimeSpan.FromSeconds(10));
        Assert.False(ResponseCachingMiddleware.ContentIsNotModified(context));

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
        context.CachedResponseHeaders = new HeaderDictionary();

        // This would fail the IfModifiedSince checks
        context.HttpContext.Request.Headers.IfModifiedSince = HeaderUtilities.FormatDate(utcNow);
        context.CachedResponseHeaders[HeaderNames.LastModified] = HeaderUtilities.FormatDate(utcNow + TimeSpan.FromSeconds(10));

        context.HttpContext.Request.Headers.IfNoneMatch = EntityTagHeaderValue.Any.ToString();
        Assert.True(ResponseCachingMiddleware.ContentIsNotModified(context));
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
        context.CachedResponseHeaders = new HeaderDictionary();

        // This would pass the IfModifiedSince checks
        context.HttpContext.Request.Headers.IfModifiedSince = HeaderUtilities.FormatDate(utcNow);
        context.CachedResponseHeaders[HeaderNames.LastModified] = HeaderUtilities.FormatDate(utcNow - TimeSpan.FromSeconds(10));

        context.HttpContext.Request.Headers.IfNoneMatch = "\"E1\"";
        Assert.False(ResponseCachingMiddleware.ContentIsNotModified(context));
        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void ContentIsNotModified_IfNoneMatch_AnyWithoutETagInResponse_False()
    {
        var sink = new TestSink();
        var context = TestUtils.CreateTestContext(sink);
        context.CachedResponseHeaders = new HeaderDictionary();
        context.HttpContext.Request.Headers.IfNoneMatch = "\"E1\"";

        Assert.False(ResponseCachingMiddleware.ContentIsNotModified(context));
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
        context.CachedResponseHeaders = new HeaderDictionary();
        context.CachedResponseHeaders[HeaderNames.ETag] = responseETag.ToString();
        context.HttpContext.Request.Headers.IfNoneMatch = requestETag.ToString();

        Assert.True(ResponseCachingMiddleware.ContentIsNotModified(context));
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.NotModifiedIfNoneMatchMatched);
    }

    [Fact]
    public void ContentIsNotModified_IfNoneMatch_ExplicitWithoutMatch_False()
    {
        var sink = new TestSink();
        var context = TestUtils.CreateTestContext(sink);
        context.CachedResponseHeaders = new HeaderDictionary();
        context.CachedResponseHeaders[HeaderNames.ETag] = "\"E2\"";
        context.HttpContext.Request.Headers.IfNoneMatch = "\"E1\"";

        Assert.False(ResponseCachingMiddleware.ContentIsNotModified(context));
        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void ContentIsNotModified_IfNoneMatch_MatchesAtLeastOneValue_True()
    {
        var sink = new TestSink();
        var context = TestUtils.CreateTestContext(sink);
        context.CachedResponseHeaders = new HeaderDictionary();
        context.CachedResponseHeaders[HeaderNames.ETag] = "\"E2\"";
        context.HttpContext.Request.Headers.IfNoneMatch = new string[] { "\"E0\", \"E1\"", "\"E1\", \"E2\"" };

        Assert.True(ResponseCachingMiddleware.ContentIsNotModified(context));
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.NotModifiedIfNoneMatchMatched);
    }

    [Fact]
    public void StartResponseAsync_IfAllowResponseCaptureIsTrue_SetsResponseTime()
    {
        var timeProvider = new FakeTimeProvider();
        var middleware = TestUtils.CreateTestMiddleware(options: new ResponseCachingOptions
        {
            TimeProvider = timeProvider
        });
        var context = TestUtils.CreateTestContext();
        context.ResponseTime = null;

        middleware.StartResponse(context);

        Assert.Equal(timeProvider.GetUtcNow(), context.ResponseTime);
    }

    [Fact]
    public void StartResponseAsync_IfAllowResponseCaptureIsTrue_SetsResponseTimeOnlyOnce()
    {
        var timeProvider = new FakeTimeProvider();
        var middleware = TestUtils.CreateTestMiddleware(options: new ResponseCachingOptions
        {
            TimeProvider = timeProvider
        });
        var context = TestUtils.CreateTestContext();
        var initialTime = timeProvider.GetUtcNow();
        context.ResponseTime = null;

        middleware.StartResponse(context);
        Assert.Equal(initialTime, context.ResponseTime);

        timeProvider.Advance(TimeSpan.FromSeconds(10));

        middleware.StartResponse(context);
        Assert.NotEqual(timeProvider.GetUtcNow(), context.ResponseTime);
        Assert.Equal(initialTime, context.ResponseTime);
    }

    [Fact]
    public void FinalizeCacheHeadersAsync_UpdateShouldCacheResponse_IfResponseCacheable()
    {
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, policyProvider: new ResponseCachingPolicyProvider());
        var context = TestUtils.CreateTestContext();

        context.HttpContext.Response.Headers.CacheControl = new CacheControlHeaderValue()
        {
            Public = true
        }.ToString();

        Assert.False(context.ShouldCacheResponse);

        middleware.FinalizeCacheHeaders(context);

        Assert.True(context.ShouldCacheResponse);
        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void FinalizeCacheHeadersAsync_DoNotUpdateShouldCacheResponse_IfResponseIsNotCacheable()
    {
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, policyProvider: new ResponseCachingPolicyProvider());
        var context = TestUtils.CreateTestContext();

        middleware.ShimResponseStream(context);

        middleware.FinalizeCacheHeaders(context);

        Assert.False(context.ShouldCacheResponse);
        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void FinalizeCacheHeadersAsync_DefaultResponseValidity_Is10Seconds()
    {
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink);
        var context = TestUtils.CreateTestContext();

        middleware.FinalizeCacheHeaders(context);

        Assert.Equal(TimeSpan.FromSeconds(10), context.CachedResponseValidFor);
        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void FinalizeCacheHeadersAsync_ResponseValidity_UseExpiryIfAvailable()
    {
        var timeProvider = new FakeTimeProvider();
        var now = timeProvider.GetUtcNow();
        now = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Offset).AddSeconds(1); // Round up to seconds.
        timeProvider.SetUtcNow(now);
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, options: new ResponseCachingOptions
        {
            TimeProvider = timeProvider
        });
        var context = TestUtils.CreateTestContext();

        context.ResponseTime = timeProvider.GetUtcNow();
        context.HttpContext.Response.Headers.Expires = HeaderUtilities.FormatDate(now + TimeSpan.FromSeconds(11));

        middleware.FinalizeCacheHeaders(context);

        Assert.Equal(TimeSpan.FromSeconds(11), context.CachedResponseValidFor);
        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void FinalizeCacheHeadersAsync_ResponseValidity_UseMaxAgeIfAvailable()
    {
        var timeProvider = new FakeTimeProvider();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, options: new ResponseCachingOptions
        {
            TimeProvider = timeProvider
        });
        var context = TestUtils.CreateTestContext();

        context.ResponseTime = timeProvider.GetUtcNow();
        context.HttpContext.Response.Headers.CacheControl = new CacheControlHeaderValue()
        {
            MaxAge = TimeSpan.FromSeconds(12)
        }.ToString();

        context.HttpContext.Response.Headers.Expires = HeaderUtilities.FormatDate(timeProvider.GetUtcNow() + TimeSpan.FromSeconds(11));

        middleware.FinalizeCacheHeaders(context);

        Assert.Equal(TimeSpan.FromSeconds(12), context.CachedResponseValidFor);
        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void FinalizeCacheHeadersAsync_ResponseValidity_UseSharedMaxAgeIfAvailable()
    {
        var timeProvider = new FakeTimeProvider();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, options: new ResponseCachingOptions
        {
            TimeProvider = timeProvider
        });
        var context = TestUtils.CreateTestContext();

        context.ResponseTime = timeProvider.GetUtcNow();
        context.HttpContext.Response.Headers.CacheControl = new CacheControlHeaderValue()
        {
            MaxAge = TimeSpan.FromSeconds(12),
            SharedMaxAge = TimeSpan.FromSeconds(13)
        }.ToString();
        context.HttpContext.Response.Headers.Expires = HeaderUtilities.FormatDate(timeProvider.GetUtcNow() + TimeSpan.FromSeconds(11));

        middleware.FinalizeCacheHeaders(context);

        Assert.Equal(TimeSpan.FromSeconds(13), context.CachedResponseValidFor);
        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void FinalizeCacheHeadersAsync_UpdateCachedVaryByRules_IfNotEquivalentToPrevious()
    {
        var cache = new TestResponseCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache);
        var context = TestUtils.CreateTestContext();

        context.HttpContext.Response.Headers.Vary = new StringValues(new[] { "headerA", "HEADERB", "HEADERc" });
        context.HttpContext.Features.Set<IResponseCachingFeature>(new ResponseCachingFeature()
        {
            VaryByQueryKeys = new StringValues(new[] { "queryB", "QUERYA" })
        });
        var cachedVaryByRules = new CachedVaryByRules()
        {
            Headers = new StringValues(new[] { "HeaderA", "HeaderB" }),
            QueryKeys = new StringValues(new[] { "QueryA", "QueryB" })
        };
        context.CachedVaryByRules = cachedVaryByRules;

        middleware.FinalizeCacheHeaders(context);

        Assert.Equal(1, cache.SetCount);
        Assert.NotSame(cachedVaryByRules, context.CachedVaryByRules);
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.VaryByRulesUpdated);
    }

    [Fact]
    public void FinalizeCacheHeadersAsync_UpdateCachedVaryByRules_IfEquivalentToPrevious()
    {
        var cache = new TestResponseCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache);
        var context = TestUtils.CreateTestContext();

        context.HttpContext.Response.Headers.Vary = new StringValues(new[] { "headerA", "HEADERB" });
        context.HttpContext.Features.Set<IResponseCachingFeature>(new ResponseCachingFeature()
        {
            VaryByQueryKeys = new StringValues(new[] { "queryB", "QUERYA" })
        });
        var cachedVaryByRules = new CachedVaryByRules()
        {
            VaryByKeyPrefix = FastGuid.NewGuid().IdString,
            Headers = new StringValues(new[] { "HEADERA", "HEADERB" }),
            QueryKeys = new StringValues(new[] { "QUERYA", "QUERYB" })
        };
        context.CachedVaryByRules = cachedVaryByRules;

        middleware.FinalizeCacheHeaders(context);

        // An update to the cache is always made but the entry should be the same
        Assert.Equal(1, cache.SetCount);
        Assert.Same(cachedVaryByRules, context.CachedVaryByRules);
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.VaryByRulesUpdated);
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
        var cache = new TestResponseCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache);
        var context = TestUtils.CreateTestContext();

        context.HttpContext.Response.Headers.Vary = vary;
        context.HttpContext.Features.Set<IResponseCachingFeature>(new ResponseCachingFeature()
        {
            VaryByQueryKeys = vary
        });

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
    public void FinalizeCacheHeadersAsync_DoNotAddDate_IfSpecified()
    {
        var utcNow = DateTimeOffset.MinValue;
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink);
        var context = TestUtils.CreateTestContext();

        context.HttpContext.Response.Headers.Date = HeaderUtilities.FormatDate(utcNow);
        context.ResponseTime = utcNow + TimeSpan.FromSeconds(10);

        Assert.Equal(HeaderUtilities.FormatDate(utcNow), context.HttpContext.Response.Headers.Date);

        middleware.FinalizeCacheHeaders(context);

        Assert.Equal(HeaderUtilities.FormatDate(utcNow), context.HttpContext.Response.Headers.Date);
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
    public void FinalizeCacheHeadersAsync_SplitsVaryHeaderByCommas()
    {
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink);
        var context = TestUtils.CreateTestContext();

        context.HttpContext.Response.Headers.Vary = "HeaderB, heaDera";

        middleware.FinalizeCacheHeaders(context);

        Assert.Equal(new StringValues(new[] { "HEADERA", "HEADERB" }), context.CachedVaryByRules.Headers);
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.VaryByRulesUpdated);
    }

    [Fact]
    public async Task FinalizeCacheBody_Cache_IfContentLengthMatches()
    {
        var cache = new TestResponseCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache);
        var context = TestUtils.CreateTestContext();

        context.ShouldCacheResponse = true;
        middleware.ShimResponseStream(context);
        context.HttpContext.Response.ContentLength = 20;

        await context.HttpContext.Response.WriteAsync(new string('0', 20));

        context.CachedResponse = new CachedResponse();
        context.BaseKey = "BaseKey";
        context.CachedResponseValidFor = TimeSpan.FromSeconds(10);

        middleware.FinalizeCacheBody(context);

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
        var cache = new TestResponseCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache);
        var context = TestUtils.CreateTestContext();

        context.ShouldCacheResponse = true;
        middleware.ShimResponseStream(context);
        context.HttpContext.Response.ContentLength = 9;
        context.HttpContext.Request.Method = method;

        await context.HttpContext.Response.WriteAsync(new string('0', 10));

        context.CachedResponse = new CachedResponse();
        context.BaseKey = "BaseKey";
        context.CachedResponseValidFor = TimeSpan.FromSeconds(10);

        middleware.FinalizeCacheBody(context);

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
        var cache = new TestResponseCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache);
        var context = TestUtils.CreateTestContext();

        context.ShouldCacheResponse = true;
        middleware.ShimResponseStream(context);
        context.HttpContext.Response.ContentLength = 10;
        context.HttpContext.Request.Method = "HEAD";

        if (includeBody)
        {
            // A response to HEAD should not include a body, but it may be present
            await context.HttpContext.Response.WriteAsync(new string('0', 10));
        }

        context.CachedResponse = new CachedResponse();
        context.BaseKey = "BaseKey";
        context.CachedResponseValidFor = TimeSpan.FromSeconds(10);

        middleware.FinalizeCacheBody(context);

        Assert.Equal(1, cache.SetCount);
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.ResponseCached);
    }

    [Fact]
    public async Task FinalizeCacheBody_Cache_IfContentLengthAbsent()
    {
        var cache = new TestResponseCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache);
        var context = TestUtils.CreateTestContext();

        context.ShouldCacheResponse = true;
        middleware.ShimResponseStream(context);

        await context.HttpContext.Response.WriteAsync(new string('0', 10));

        context.CachedResponse = new CachedResponse()
        {
            Headers = new HeaderDictionary()
        };
        context.BaseKey = "BaseKey";
        context.CachedResponseValidFor = TimeSpan.FromSeconds(10);

        middleware.FinalizeCacheBody(context);

        Assert.Equal(1, cache.SetCount);
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.ResponseCached);
    }

    [Fact]
    public async Task FinalizeCacheBody_DoNotCache_IfShouldCacheResponseFalse()
    {
        var cache = new TestResponseCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache);
        var context = TestUtils.CreateTestContext();

        middleware.ShimResponseStream(context);
        await context.HttpContext.Response.WriteAsync(new string('0', 10));
        context.ShouldCacheResponse = false;

        middleware.FinalizeCacheBody(context);

        Assert.Equal(0, cache.SetCount);
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.ResponseNotCached);
    }

    [Fact]
    public async Task FinalizeCacheBody_DoNotCache_IfBufferingDisabled()
    {
        var cache = new TestResponseCache();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache);
        var context = TestUtils.CreateTestContext();

        context.ShouldCacheResponse = true;
        middleware.ShimResponseStream(context);
        await context.HttpContext.Response.WriteAsync(new string('0', 10));

        context.ResponseCachingStream.DisableBuffering();

        middleware.FinalizeCacheBody(context);

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
            cache: new MemoryResponseCache(new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = 100
            })));
        var context = TestUtils.CreateTestContext();

        context.ShouldCacheResponse = true;
        middleware.ShimResponseStream(context);

        await context.HttpContext.Response.WriteAsync(new string('0', 101));

        context.CachedResponse = new CachedResponse() { Headers = new HeaderDictionary() };
        context.CachedResponseValidFor = TimeSpan.FromSeconds(10);

        middleware.FinalizeCacheBody(context);

        // The response cached message will be logged but the adding of the entry will no-op
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.ResponseCached);

        // The entry cannot be retrieved
        Assert.False(await middleware.TryServeFromCacheAsync(context));
    }

    [Fact]
    public void AddResponseCachingFeature_SecondInvocation_Throws()
    {
        var httpContext = new DefaultHttpContext();

        // Should not throw
        ResponseCachingMiddleware.AddResponseCachingFeature(httpContext);

        // Should throw
        Assert.ThrowsAny<InvalidOperationException>(() => ResponseCachingMiddleware.AddResponseCachingFeature(httpContext));
    }

    private class FakeResponseFeature : HttpResponseFeature
    {
        public override void OnStarting(Func<object, Task> callback, object state) { }
    }

    [Theory]
    // If allowResponseCaching is false, other settings will not matter but are included for completeness
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public async Task Invoke_AddsResponseCachingFeature_Always(bool allowResponseCaching, bool allowCacheLookup, bool allowCacheStorage)
    {
        var responseCachingFeatureAdded = false;
        var middleware = TestUtils.CreateTestMiddleware(next: httpContext =>
        {
            responseCachingFeatureAdded = httpContext.Features.Get<IResponseCachingFeature>() != null;
            return Task.CompletedTask;
        },
        policyProvider: new TestResponseCachingPolicyProvider
        {
            AttemptResponseCachingValue = allowResponseCaching,
            AllowCacheLookupValue = allowCacheLookup,
            AllowCacheStorageValue = allowCacheStorage
        });

        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(new FakeResponseFeature());
        await middleware.Invoke(context);

        Assert.True(responseCachingFeatureAdded);
    }

    [Fact]
    public void GetOrderCasingNormalizedStringValues_NormalizesCasingToUpper()
    {
        var uppercaseStrings = new StringValues(new[] { "STRINGA", "STRINGB" });
        var lowercaseStrings = new StringValues(new[] { "stringA", "stringB" });

        var normalizedStrings = ResponseCachingMiddleware.GetOrderCasingNormalizedStringValues(lowercaseStrings);

        Assert.Equal(uppercaseStrings, normalizedStrings);
    }

    [Fact]
    public void GetOrderCasingNormalizedStringValues_NormalizesOrder()
    {
        var orderedStrings = new StringValues(new[] { "STRINGA", "STRINGB" });
        var reverseOrderStrings = new StringValues(new[] { "STRINGB", "STRINGA" });

        var normalizedStrings = ResponseCachingMiddleware.GetOrderCasingNormalizedStringValues(reverseOrderStrings);

        Assert.Equal(orderedStrings, normalizedStrings);
    }

    [Fact]
    public void GetOrderCasingNormalizedStringValues_PreservesCommas()
    {
        var originalStrings = new StringValues(new[] { "STRINGA, STRINGB" });

        var normalizedStrings = ResponseCachingMiddleware.GetOrderCasingNormalizedStringValues(originalStrings);

        Assert.Equal(originalStrings, normalizedStrings);
    }
}
