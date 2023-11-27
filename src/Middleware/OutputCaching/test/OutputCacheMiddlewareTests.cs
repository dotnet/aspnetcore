// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.OutputCaching.Memory;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.OutputCaching.Tests;

public class OutputCacheMiddlewareTests_SimpleStore : OutputCacheMiddlewareTests
{
    public override ITestOutputCacheStore GetStore() => new SimpleTestOutputCache();
}

public class OutputCacheMiddlewareTests_BufferStore : OutputCacheMiddlewareTests
{
    public override ITestOutputCacheStore GetStore() => new BufferTestOutputCache();
}

public abstract class OutputCacheMiddlewareTests
{
    public abstract ITestOutputCacheStore GetStore();

    [Fact]
    public async Task TryServeFromCacheAsync_OnlyIfCached_Serves504()
    {
        var cache = GetStore();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache, keyProvider: new TestResponseCachingKeyProvider("BaseKey"));
        var context = TestUtils.CreateTestContext(cache: cache);
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
        var cache = GetStore();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache, keyProvider: new TestResponseCachingKeyProvider("BaseKey"));
        var context = TestUtils.CreateTestContext(cache: cache);
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
        var cache = GetStore();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache, keyProvider: new TestResponseCachingKeyProvider("BaseKey"));
        var context = TestUtils.CreateTestContext(cache: cache);
        middleware.TryGetRequestPolicies(context.HttpContext, out var policies);

        using (var entry = new OutputCacheEntry(DateTimeOffset.UtcNow, StatusCodes.Status200OK))
        {
            await OutputCacheEntryFormatter.StoreAsync(
                "BaseKey",
                entry,
                null,
                TimeSpan.Zero,
                cache,
                NullLogger.Instance,
                default);
        }

        Assert.True(await middleware.TryServeFromCacheAsync(context, policies));
        Assert.Equal(1, cache.GetCount);
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.CachedResponseServed);
    }

    [Fact]
    public async Task TryServeFromCacheAsync_CachedResponseFound_OverwritesExistingHeaders()
    {
        var cache = GetStore();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache, keyProvider: new TestResponseCachingKeyProvider("BaseKey"));
        var context = TestUtils.CreateTestContext(cache: cache);
        middleware.TryGetRequestPolicies(context.HttpContext, out var policies);
        context.CacheKey = "BaseKey";

        context.HttpContext.Response.Headers["MyHeader"] = "OldValue";
        using (var entry = new OutputCacheEntry(DateTimeOffset.UtcNow, StatusCodes.Status200OK)
                .CopyHeadersFrom(new HeaderDictionary() { { "MyHeader", "NewValue" } }))
        {
            await OutputCacheEntryFormatter.StoreAsync(context.CacheKey,
                entry,
                null,
                TimeSpan.Zero,
                cache,
                NullLogger.Instance,
                default);
        }
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
        var cache = GetStore();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache, keyProvider: new TestResponseCachingKeyProvider("BaseKey"));
        var context = TestUtils.CreateTestContext(cache: cache);
        context.HttpContext.Request.Headers.IfNoneMatch = "*";
        middleware.TryGetRequestPolicies(context.HttpContext, out var policies);

        using (var entry = new OutputCacheEntry(DateTimeOffset.UtcNow, StatusCodes.Status200OK))
        {
            await OutputCacheEntryFormatter.StoreAsync("BaseKey",
                entry,
                null,
                TimeSpan.Zero,
                cache,
                NullLogger.Instance,
                default);
        }

        Assert.True(await middleware.TryServeFromCacheAsync(context, policies));
        Assert.Equal(1, cache.GetCount);
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.NotModifiedIfNoneMatchStar,
            LoggedMessage.NotModifiedServed);
    }

    [Fact]
    public void ContentIsNotModified_NotConditionalRequest_False()
    {
        var cache = GetStore();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache);
        var context = TestUtils.CreateTestContext(testSink: sink);
        using var entry = new OutputCacheEntry(DateTimeOffset.UtcNow, StatusCodes.Status200OK);
        context.CachedResponse = entry;

        Assert.False(middleware.ContentIsNotModified(context));
        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void ContentIsNotModified_IfModifiedSince_FallsBackToDateHeader()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var sink = new TestSink();
        var context = TestUtils.CreateTestContext(testSink: sink);
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink);
        using var entry = new OutputCacheEntry(DateTimeOffset.UtcNow, StatusCodes.Status200OK);
        context.CachedResponse = entry;

        context.HttpContext.Request.Headers.IfModifiedSince = HeaderUtilities.FormatDate(utcNow);

        static void SetDateHeader(OutputCacheEntry entry, DateTimeOffset value)
        {
            entry.CopyHeadersFrom(new HeaderDictionary { [HeaderNames.Date] = HeaderUtilities.FormatDate(value) });
        }

        // Verify modifications in the past succeeds
        SetDateHeader(context.CachedResponse, utcNow - TimeSpan.FromSeconds(10));
        Assert.True(middleware.ContentIsNotModified(context));
        Assert.Single(sink.Writes);

        // Verify modifications at present succeeds
        SetDateHeader(context.CachedResponse, utcNow);
        Assert.True(middleware.ContentIsNotModified(context));
        Assert.Equal(2, sink.Writes.Count);

        // Verify modifications in the future fails
        SetDateHeader(context.CachedResponse, utcNow + TimeSpan.FromSeconds(10));
        Assert.False(middleware.ContentIsNotModified(context));

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
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink);
        var context = TestUtils.CreateTestContext(testSink: sink);
        using var entry = new OutputCacheEntry(DateTimeOffset.UtcNow, StatusCodes.Status200OK);
        context.CachedResponse = entry;

        context.HttpContext.Request.Headers.IfModifiedSince = HeaderUtilities.FormatDate(utcNow);

        static void SetDateHeaders(OutputCacheEntry entry, DateTimeOffset date, DateTimeOffset lastModified)
        {
            entry.CopyHeadersFrom(new HeaderDictionary
            {
                [HeaderNames.Date] = HeaderUtilities.FormatDate(date),
                [HeaderNames.LastModified] = HeaderUtilities.FormatDate(lastModified),
            });
        }

        // Verify modifications in the past succeeds
        SetDateHeaders(context.CachedResponse, utcNow + TimeSpan.FromSeconds(10), utcNow - TimeSpan.FromSeconds(10));
        Assert.True(middleware.ContentIsNotModified(context));
        Assert.Single(sink.Writes);

        // Verify modifications at present
        SetDateHeaders(context.CachedResponse, utcNow + TimeSpan.FromSeconds(10), utcNow);
        Assert.True(middleware.ContentIsNotModified(context));
        Assert.Equal(2, sink.Writes.Count);

        // Verify modifications in the future fails
        SetDateHeaders(context.CachedResponse, utcNow - TimeSpan.FromSeconds(10), utcNow + TimeSpan.FromSeconds(10));
        Assert.False(middleware.ContentIsNotModified(context));

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
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink);
        var context = TestUtils.CreateTestContext(testSink: sink);
        using var entry = new OutputCacheEntry(DateTimeOffset.UtcNow, StatusCodes.Status200OK);
        context.CachedResponse = entry;

        // This would fail the IfModifiedSince checks
        context.HttpContext.Request.Headers.IfModifiedSince = HeaderUtilities.FormatDate(utcNow);
        entry.CopyHeadersFrom(new HeaderDictionary { [HeaderNames.LastModified] = HeaderUtilities.FormatDate(utcNow + TimeSpan.FromSeconds(10)) });

        context.HttpContext.Request.Headers.IfNoneMatch = EntityTagHeaderValue.Any.ToString();
        Assert.True(middleware.ContentIsNotModified(context));
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.NotModifiedIfNoneMatchStar);
    }

    [Fact]
    public void ContentIsNotModified_IfNoneMatch_Overrides_IfModifiedSince_ToFalse()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink);
        var context = TestUtils.CreateTestContext(testSink: sink);
        using var entry = new OutputCacheEntry(DateTimeOffset.UtcNow, StatusCodes.Status200OK);
        context.CachedResponse = entry;

        // This would pass the IfModifiedSince checks
        context.HttpContext.Request.Headers.IfModifiedSince = HeaderUtilities.FormatDate(utcNow);
        context.CachedResponse.CopyHeadersFrom(new HeaderDictionary { [HeaderNames.LastModified] = HeaderUtilities.FormatDate(utcNow - TimeSpan.FromSeconds(10)) });

        context.HttpContext.Request.Headers.IfNoneMatch = "\"E1\"";
        Assert.False(middleware.ContentIsNotModified(context));
        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void ContentIsNotModified_IfNoneMatch_AnyWithoutETagInResponse_False()
    {
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink);
        var context = TestUtils.CreateTestContext(testSink: sink);
        using var entry = new OutputCacheEntry(DateTimeOffset.UtcNow, StatusCodes.Status200OK);
        context.CachedResponse = entry;
        context.HttpContext.Request.Headers.IfNoneMatch = "\"E1\"";

        Assert.False(middleware.ContentIsNotModified(context));
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
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink);
        var context = TestUtils.CreateTestContext(testSink: sink);
        using var entry = new OutputCacheEntry(DateTimeOffset.UtcNow, StatusCodes.Status200OK)
            .CopyHeadersFrom(new HeaderDictionary { [HeaderNames.ETag] = responseETag.ToString() });
        context.CachedResponse = entry;
        context.HttpContext.Request.Headers.IfNoneMatch = requestETag.ToString();

        Assert.True(middleware.ContentIsNotModified(context));
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.NotModifiedIfNoneMatchMatched);
    }

    [Fact]
    public void ContentIsNotModified_IfNoneMatch_ExplicitWithoutMatch_False()
    {
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink);
        var context = TestUtils.CreateTestContext(testSink: sink);
        using var entry = new OutputCacheEntry(DateTimeOffset.UtcNow, StatusCodes.Status200OK);
        context.CachedResponse = entry;
        context.CachedResponse.CopyHeadersFrom(new HeaderDictionary { [HeaderNames.ETag] = "\"E2\"" });
        context.HttpContext.Request.Headers.IfNoneMatch = "\"E1\"";

        Assert.False(middleware.ContentIsNotModified(context));
        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void ContentIsNotModified_IfNoneMatch_MatchesAtLeastOneValue_True()
    {
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink);
        var context = TestUtils.CreateTestContext(testSink: sink);
        using var entry = new OutputCacheEntry(DateTimeOffset.UtcNow, StatusCodes.Status200OK);
        context.CachedResponse = entry;
        context.CachedResponse.CopyHeadersFrom(new HeaderDictionary { [HeaderNames.ETag] = "\"E2\"" });
        context.HttpContext.Request.Headers.IfNoneMatch = new string[] { "\"E0\", \"E1\"", "\"E1\", \"E2\"" };

        Assert.True(middleware.ContentIsNotModified(context));
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.NotModifiedIfNoneMatchMatched);
    }

    [Fact]
    public void StartResponseAsync_IfAllowResponseCaptureIsTrue_SetsResponseTime()
    {
        var timeProvider = new FakeTimeProvider();
        var middleware = TestUtils.CreateTestMiddleware(options: new OutputCacheOptions
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
        var middleware = TestUtils.CreateTestMiddleware(options: new OutputCacheOptions
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

        var timeProvider = new FakeTimeProvider();
        var options = new OutputCacheOptions
        {
            TimeProvider = timeProvider
        };
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, options: options);
        var context = TestUtils.CreateTestContext();

        context.ResponseTime = timeProvider.GetUtcNow();
        context.HttpContext.Response.Headers.Expires = HeaderUtilities.FormatDate(timeProvider.GetUtcNow() + TimeSpan.FromSeconds(11));

        middleware.FinalizeCacheHeaders(context);

        Assert.Equal(options.DefaultExpirationTimeSpan, context.CachedResponseValidFor);
        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void FinalizeCacheHeadersAsync_ResponseValidity_UseMaxAgeIfAvailable()
    {
        // The MaxAge header should not be used if set in the response

        var timeProvider = new FakeTimeProvider();
        var sink = new TestSink();
        var options = new OutputCacheOptions
        {
            TimeProvider = timeProvider
        };
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, options: options);
        var context = TestUtils.CreateTestContext();

        context.ResponseTime = timeProvider.GetUtcNow();
        context.HttpContext.Response.Headers.CacheControl = new CacheControlHeaderValue()
        {
            MaxAge = TimeSpan.FromSeconds(12)
        }.ToString();

        context.HttpContext.Response.Headers.Expires = HeaderUtilities.FormatDate(timeProvider.GetUtcNow() + TimeSpan.FromSeconds(11));

        middleware.FinalizeCacheHeaders(context);

        Assert.Equal(options.DefaultExpirationTimeSpan, context.CachedResponseValidFor);
        Assert.Empty(sink.Writes);
    }

    [Fact]
    public void FinalizeCacheHeadersAsync_ResponseValidity_UseSharedMaxAgeIfAvailable()
    {
        var timeProvider = new FakeTimeProvider();
        var sink = new TestSink();
        var options = new OutputCacheOptions
        {
            TimeProvider = timeProvider
        };
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, options: options);
        var context = TestUtils.CreateTestContext();

        context.ResponseTime = timeProvider.GetUtcNow();
        context.HttpContext.Response.Headers.CacheControl = new CacheControlHeaderValue()
        {
            MaxAge = TimeSpan.FromSeconds(12),
            SharedMaxAge = TimeSpan.FromSeconds(13)
        }.ToString();
        context.HttpContext.Response.Headers.Expires = HeaderUtilities.FormatDate(timeProvider.GetUtcNow() + TimeSpan.FromSeconds(11));

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
        var cache = GetStore();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache);
        var context = TestUtils.CreateTestContext(cache: cache);

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

        Assert.Equal(new StringValues(new[] { "HeaderB, heaDera" }), context.CachedResponse.FindHeader(HeaderNames.Vary));
    }

    [Fact]
    public async Task FinalizeCacheBody_Cache_IfContentLengthMatches()
    {
        var cache = GetStore();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache);
        var context = TestUtils.CreateTestContext(cache: cache);

        middleware.ShimResponseStream(context);
        context.HttpContext.Response.ContentLength = 20;

        await context.HttpContext.Response.WriteAsync(new string('0', 20));

        using var entry = new OutputCacheEntry(DateTimeOffset.UtcNow, StatusCodes.Status200OK);
        context.CachedResponse = entry;
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
        var cache = GetStore();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache);
        var context = TestUtils.CreateTestContext(cache: cache);

        middleware.ShimResponseStream(context);
        context.HttpContext.Response.ContentLength = 9;
        context.HttpContext.Request.Method = method;

        await context.HttpContext.Response.WriteAsync(new string('0', 10));

        using var entry = new OutputCacheEntry(DateTimeOffset.UtcNow, StatusCodes.Status200OK);
        context.CachedResponse = entry;
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
        var cache = GetStore();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache);
        var context = TestUtils.CreateTestContext(cache: cache);

        middleware.ShimResponseStream(context);
        context.HttpContext.Response.ContentLength = 10;
        context.HttpContext.Request.Method = "HEAD";

        if (includeBody)
        {
            // A response to HEAD should not include a body, but it may be present
            await context.HttpContext.Response.WriteAsync(new string('0', 10));
        }

        using var entry = new OutputCacheEntry(DateTimeOffset.UtcNow, StatusCodes.Status200OK);
        context.CachedResponse = entry;
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
        var cache = GetStore();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache);
        var context = TestUtils.CreateTestContext(cache: cache);

        middleware.ShimResponseStream(context);

        await context.HttpContext.Response.WriteAsync(new string('0', 10));

        using var entry = new OutputCacheEntry(DateTimeOffset.UtcNow, StatusCodes.Status200OK);
        context.CachedResponse = entry;
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
        var cache = GetStore();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache);
        var context = TestUtils.CreateTestContext(cache: cache);

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
        var cache = GetStore();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache);
        var context = TestUtils.CreateTestContext(cache: cache);

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

        using var entry = new OutputCacheEntry(DateTimeOffset.UtcNow, StatusCodes.Status200OK);
        context.CachedResponse = entry;
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
    public async Task Locking_PreventsConcurrentRequests()
    {
        var responseCounter = 0;

        var task1Executing = new ManualResetEventSlim(false);
        var task2Executing = new ManualResetEventSlim(false);

        var options = new OutputCacheOptions();
        options.AddBasePolicy(build => build.Cache());

        var middleware = TestUtils.CreateTestMiddleware(options: options, next: async c =>
        {
            responseCounter++;
            task1Executing.Set();

            // Wait for the second request to start before processing the first one
            task2Executing.Wait();

            // Simulate some delay to allow for the second request to run while this one is pending
            await Task.Delay(500);
        });

        var context1 = TestUtils.CreateTestContext();
        context1.HttpContext.Request.Method = "GET";
        context1.HttpContext.Request.Path = "/";

        var context2 = TestUtils.CreateTestContext();
        context2.HttpContext.Request.Method = "GET";
        context2.HttpContext.Request.Path = "/";

        var task1 = Task.Run(() => middleware.Invoke(context1.HttpContext));

        // Wait for the first request to be processed before sending a second one
        task1Executing.Wait();

        var task2 = Task.Run(() => middleware.Invoke(context2.HttpContext));

        task2Executing.Set();

        await Task.WhenAll(task1, task2);

        Assert.Equal(1, responseCounter);
    }

    [Fact]
    public async Task Locking_IgnoresNonCacheableResponses()
    {
        var responseCounter = 0;

        var blocker1 = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var blocker2 = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var memoryStream1 = new MemoryStream();
        var memoryStream2 = new MemoryStream();

        var options = new OutputCacheOptions();
        options.AddBasePolicy(build => build.Cache());

        var middleware = TestUtils.CreateTestMiddleware(options: options, next: async c =>
        {
            responseCounter++;

            if (responseCounter == 1)
            {
                blocker1.SetResult(true);
            }

            c.Response.Cookies.Append("a", "b");
            c.Response.Write("Hello" + responseCounter);

            await blocker2.Task;
        });

        var context1 = TestUtils.CreateTestContext();
        context1.HttpContext.Request.Method = "GET";
        context1.HttpContext.Request.Path = "/";
        context1.HttpContext.Response.Body = memoryStream1;

        var context2 = TestUtils.CreateTestContext();
        context2.HttpContext.Request.Method = "GET";
        context2.HttpContext.Request.Path = "/";
        context2.HttpContext.Response.Body = memoryStream2;

        var task1 = Task.Run(() => middleware.Invoke(context1.HttpContext));

        // Wait for context1 to be processed
        await blocker1.Task;

        // Start context2
        var task2 = Task.Run(() => middleware.Invoke(context2.HttpContext));

        // Wait for it to be blocked by the locking feature
        await Task.Delay(500);

        // Unblock context1
        blocker2.SetResult(true);

        await Task.WhenAll(task1, task2);

        Assert.Equal(2, responseCounter);

        // Ensure that even though two requests were processed, no result was returned from cache
        Assert.Equal("Hello1", Encoding.UTF8.GetString(memoryStream1.ToArray()));
        Assert.Equal("Hello2", Encoding.UTF8.GetString(memoryStream2.ToArray()));
    }

    [Fact]
    public async Task Locking_ExecuteAllRequestsWhenDisabled()
    {
        var responseCounter = 0;

        var blocker1 = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var blocker2 = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var options = new OutputCacheOptions();
        options.AddBasePolicy(build => build.Cache().SetLocking(false));

        var middleware = TestUtils.CreateTestMiddleware(options: options, next: async c =>
        {
            responseCounter++;

            switch (responseCounter)
            {
                case 1:
                    blocker1.SetResult(true);
                    await blocker2.Task;
                    break;
                case 2:
                    await blocker1.Task;
                    blocker2.SetResult(true);
                    break;
            }

            c.Response.Write("Hello" + responseCounter);
        });

        var context1 = TestUtils.CreateTestContext();
        context1.HttpContext.Request.Method = "GET";
        context1.HttpContext.Request.Path = "/";

        var context2 = TestUtils.CreateTestContext();
        context2.HttpContext.Request.Method = "GET";
        context2.HttpContext.Request.Path = "/";

        var task1 = Task.Run(() => middleware.Invoke(context1.HttpContext));

        var task2 = Task.Run(() => middleware.Invoke(context2.HttpContext));

        await Task.WhenAll(task1, task2);

        Assert.Equal(2, responseCounter);
    }

    [Fact]
    public async Task EmptyCacheKey_IsNotCached()
    {
        var cache = GetStore();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(testSink: sink, cache: cache);
        var context = TestUtils.CreateTestContext(cache: cache);

        middleware.ShimResponseStream(context);
        context.HttpContext.Response.ContentLength = 5;
        context.HttpContext.Request.Method = "GET";

        // A response to HEAD should not include a body, but it may be present
        await context.HttpContext.Response.WriteAsync("Hello");

        using var entry = new OutputCacheEntry(DateTimeOffset.UtcNow, StatusCodes.Status200OK);
        context.CachedResponse = entry;
        context.CacheKey = "";
        context.CachedResponseValidFor = TimeSpan.FromSeconds(10);

        await middleware.FinalizeCacheBodyAsync(context);

        Assert.Equal(1, cache.SetCount);
        TestUtils.AssertLoggedMessages(
            sink.Writes,
            LoggedMessage.ResponseCached);
    }

    public class RefreshableCachePolicy : IOutputCachePolicy
    {
        public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation)
        {
            context.AllowCacheLookup = !context.HttpContext.Request.Headers.ContainsKey("X-Refresh");
            context.AllowCacheStorage = true;
            return ValueTask.CompletedTask;
        }

        public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation)
        {
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task Can_Implement_Policy_That_Enables_Storage_Without_Serving()
    {
        var options = new OutputCacheOptions();
        options.AddBasePolicy(builder =>
        {
            builder.AddPolicy(new RefreshableCachePolicy());
            builder.Cache();
        }, true);

        var cache = GetStore();
        var sink = new TestSink();
        var middleware = TestUtils.CreateTestMiddleware(options: options, testSink: sink, cache: cache, next: async c =>
        {
            await c.Response.WriteAsync(Guid.NewGuid().ToString());
        });

        // Act - Four requests are executed. The third request
        //       should trigger a cache refresh so that the first two requests
        //       have matching output, and the last two have matching output.
        var initialResponse = await SendRequestAsync(includeRefreshHeader: false);
        var cachedResponse = await SendRequestAsync(includeRefreshHeader: false);
        var refreshedResponse = await SendRequestAsync(includeRefreshHeader: true);
        var cachedResponseAfterRefresh = await SendRequestAsync(includeRefreshHeader: false);

        Assert.Equal(initialResponse, cachedResponse);
        Assert.NotEqual(cachedResponse, refreshedResponse);
        Assert.Equal(refreshedResponse, cachedResponseAfterRefresh);

        async Task<string> SendRequestAsync(bool includeRefreshHeader)
        {
            var requestContext = TestUtils.CreateTestContext(cache: cache);
            requestContext.HttpContext.Request.Method = "GET";
            requestContext.HttpContext.Request.Path = "/";
            var responseStream = new MemoryStream();
            requestContext.HttpContext.Response.Body = responseStream;

            if (includeRefreshHeader)
            {
                requestContext.HttpContext.Request.Headers.Add("X-Refresh", "randomvalue");
            }

            await middleware.Invoke(requestContext.HttpContext);
            var response = Encoding.UTF8.GetString(responseStream.GetBuffer());
            return response;
        }
    }
}
