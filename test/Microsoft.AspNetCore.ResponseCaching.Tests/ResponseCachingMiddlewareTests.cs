// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.ResponseCaching.Internal;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.ResponseCaching.Tests
{
    public class ResponseCachingMiddlewareTests
    {
        [Fact]
        public async Task TryServeFromCacheAsync_OnlyIfCached_Serves504()
        {
            var cache = new TestResponseCache();
            var middleware = TestUtils.CreateTestMiddleware(responseCache: cache, cacheKeyProvider: new TestKeyProvider());
            var context = TestUtils.CreateTestContext();
            context.TypedRequestHeaders.CacheControl = new CacheControlHeaderValue()
            {
                OnlyIfCached = true
            };

            Assert.True(await middleware.TryServeFromCacheAsync(context));
            Assert.Equal(StatusCodes.Status504GatewayTimeout, context.HttpContext.Response.StatusCode);
        }

        [Fact]
        public async Task TryServeFromCacheAsync_CachedResponseNotFound_Fails()
        {
            var cache = new TestResponseCache();
            var middleware = TestUtils.CreateTestMiddleware(responseCache: cache, cacheKeyProvider: new TestKeyProvider(new[] { "BaseKey", "BaseKey2" }));
            var context = TestUtils.CreateTestContext();

            Assert.False(await middleware.TryServeFromCacheAsync(context));
            Assert.Equal(2, cache.GetCount);
        }

        [Fact]
        public async Task TryServeFromCacheAsync_CachedResponseFound_Succeeds()
        {
            var cache = new TestResponseCache();
            var middleware = TestUtils.CreateTestMiddleware(responseCache: cache, cacheKeyProvider: new TestKeyProvider(new[] { "BaseKey", "BaseKey2" }));
            var context = TestUtils.CreateTestContext();

            cache.Set(
                "BaseKey2",
                new CachedResponse()
                {
                    Body = new byte[0]
                },
                TimeSpan.Zero);

            Assert.True(await middleware.TryServeFromCacheAsync(context));
            Assert.Equal(2, cache.GetCount);
        }

        [Fact]
        public async Task TryServeFromCacheAsync_VaryRuleFound_CachedResponseNotFound_Fails()
        {
            var cache = new TestResponseCache();
            var middleware = TestUtils.CreateTestMiddleware(responseCache: cache, cacheKeyProvider: new TestKeyProvider(new[] { "BaseKey", "BaseKey2" }));
            var context = TestUtils.CreateTestContext();

            cache.Set(
                "BaseKey2",
                new CachedVaryRules(),
                TimeSpan.Zero);

            Assert.False(await middleware.TryServeFromCacheAsync(context));
            Assert.Equal(2, cache.GetCount);
        }

        [Fact]
        public async Task TryServeFromCacheAsync_VaryRuleFound_CachedResponseFound_Succeeds()
        {
            var cache = new TestResponseCache();
            var middleware = TestUtils.CreateTestMiddleware(responseCache: cache, cacheKeyProvider: new TestKeyProvider(new[] { "BaseKey", "BaseKey2" }, new[] { "VaryKey", "VaryKey2" }));
            var context = TestUtils.CreateTestContext();

            cache.Set(
                "BaseKey2",
                new CachedVaryRules(),
                TimeSpan.Zero);
            cache.Set(
                "BaseKey2VaryKey2",
                new CachedResponse()
                {
                    Body = new byte[0]
                },
                TimeSpan.Zero);

            Assert.True(await middleware.TryServeFromCacheAsync(context));
            Assert.Equal(6, cache.GetCount);
        }

        [Fact]
        public void ConditionalRequestSatisfied_NotConditionalRequest_Fails()
        {
            var context = TestUtils.CreateTestContext();
            context.CachedResponseHeaders = new ResponseHeaders(new HeaderDictionary());

            Assert.False(ResponseCachingMiddleware.ConditionalRequestSatisfied(context));
        }

        [Fact]
        public void ConditionalRequestSatisfied_IfUnmodifiedSince_FallsbackToDateHeader()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var context = TestUtils.CreateTestContext();
            context.CachedResponseHeaders = new ResponseHeaders(new HeaderDictionary());

            context.TypedRequestHeaders.IfUnmodifiedSince = utcNow;

            // Verify modifications in the past succeeds
            context.CachedResponseHeaders.Date = utcNow - TimeSpan.FromSeconds(10);
            Assert.True(ResponseCachingMiddleware.ConditionalRequestSatisfied(context));

            // Verify modifications at present succeeds
            context.CachedResponseHeaders.Date = utcNow;
            Assert.True(ResponseCachingMiddleware.ConditionalRequestSatisfied(context));

            // Verify modifications in the future fails
            context.CachedResponseHeaders.Date = utcNow + TimeSpan.FromSeconds(10);
            Assert.False(ResponseCachingMiddleware.ConditionalRequestSatisfied(context));
        }

        [Fact]
        public void ConditionalRequestSatisfied_IfUnmodifiedSince_LastModifiedOverridesDateHeader()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var context = TestUtils.CreateTestContext();
            context.CachedResponseHeaders = new ResponseHeaders(new HeaderDictionary());

            context.TypedRequestHeaders.IfUnmodifiedSince = utcNow;

            // Verify modifications in the past succeeds
            context.CachedResponseHeaders.Date = utcNow + TimeSpan.FromSeconds(10);
            context.CachedResponseHeaders.LastModified = utcNow - TimeSpan.FromSeconds(10);
            Assert.True(ResponseCachingMiddleware.ConditionalRequestSatisfied(context));

            // Verify modifications at present
            context.CachedResponseHeaders.Date = utcNow + TimeSpan.FromSeconds(10);
            context.CachedResponseHeaders.LastModified = utcNow;
            Assert.True(ResponseCachingMiddleware.ConditionalRequestSatisfied(context));

            // Verify modifications in the future fails
            context.CachedResponseHeaders.Date = utcNow - TimeSpan.FromSeconds(10);
            context.CachedResponseHeaders.LastModified = utcNow + TimeSpan.FromSeconds(10);
            Assert.False(ResponseCachingMiddleware.ConditionalRequestSatisfied(context));
        }

        [Fact]
        public void ConditionalRequestSatisfied_IfNoneMatch_Overrides_IfUnmodifiedSince_ToPass()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var context = TestUtils.CreateTestContext();
            context.CachedResponseHeaders = new ResponseHeaders(new HeaderDictionary());

            // This would fail the IfUnmodifiedSince checks
            context.TypedRequestHeaders.IfUnmodifiedSince = utcNow;
            context.CachedResponseHeaders.LastModified = utcNow + TimeSpan.FromSeconds(10);

            context.TypedRequestHeaders.IfNoneMatch = new List<EntityTagHeaderValue>(new[] { EntityTagHeaderValue.Any });
            Assert.True(ResponseCachingMiddleware.ConditionalRequestSatisfied(context));
        }

        [Fact]
        public void ConditionalRequestSatisfied_IfNoneMatch_Overrides_IfUnmodifiedSince_ToFail()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var context = TestUtils.CreateTestContext();
            context.CachedResponseHeaders = new ResponseHeaders(new HeaderDictionary());

            // This would pass the IfUnmodifiedSince checks
            context.TypedRequestHeaders.IfUnmodifiedSince = utcNow;
            context.CachedResponseHeaders.LastModified = utcNow - TimeSpan.FromSeconds(10);

            context.TypedRequestHeaders.IfNoneMatch = new List<EntityTagHeaderValue>(new[] { new EntityTagHeaderValue("\"E1\"") });
            Assert.False(ResponseCachingMiddleware.ConditionalRequestSatisfied(context));
        }

        [Fact]
        public void ConditionalRequestSatisfied_IfNoneMatch_AnyWithoutETagInResponse_Passes()
        {
            var context = TestUtils.CreateTestContext();
            context.CachedResponseHeaders = new ResponseHeaders(new HeaderDictionary());

            context.TypedRequestHeaders.IfNoneMatch = new List<EntityTagHeaderValue>(new[] { new EntityTagHeaderValue("\"E1\"") });

            Assert.False(ResponseCachingMiddleware.ConditionalRequestSatisfied(context));
        }

        [Fact]
        public void ConditionalRequestSatisfied_IfNoneMatch_ExplicitWithMatch_Passes()
        {
            var context = TestUtils.CreateTestContext();
            context.CachedResponseHeaders = new ResponseHeaders(new HeaderDictionary())
            {
                ETag = new EntityTagHeaderValue("\"E1\"")
            };

            context.TypedRequestHeaders.IfNoneMatch = new List<EntityTagHeaderValue>(new[] { new EntityTagHeaderValue("\"E1\"") });

            Assert.True(ResponseCachingMiddleware.ConditionalRequestSatisfied(context));
        }

        [Fact]
        public void ConditionalRequestSatisfied_IfNoneMatch_ExplicitWithoutMatch_Fails()
        {
            var context = TestUtils.CreateTestContext();
            context.CachedResponseHeaders = new ResponseHeaders(new HeaderDictionary())
            {
                ETag = new EntityTagHeaderValue("\"E2\"")
            };

            context.TypedRequestHeaders.IfNoneMatch = new List<EntityTagHeaderValue>(new[] { new EntityTagHeaderValue("\"E1\"") });

            Assert.False(ResponseCachingMiddleware.ConditionalRequestSatisfied(context));
        }

        [Fact]
        public void FinalizeCachingHeaders_DoNotUpdateShouldCacheResponse_IfResponseIsNotCacheable()
        {
            var middleware = TestUtils.CreateTestMiddleware(cacheabilityValidator: new CacheabilityValidator());
            var context = TestUtils.CreateTestContext();

            Assert.False(context.ShouldCacheResponse);

            middleware.ShimResponseStream(context);
            middleware.FinalizeCachingHeaders(context);

            Assert.False(context.ShouldCacheResponse);
        }

        [Fact]
        public void FinalizeCachingHeaders_UpdateShouldCacheResponse_IfResponseIsCacheable()
        {
            var middleware = TestUtils.CreateTestMiddleware(cacheabilityValidator: new CacheabilityValidator());
            var context = TestUtils.CreateTestContext();
            context.TypedResponseHeaders.CacheControl = new CacheControlHeaderValue()
            {
                Public = true
            };

            Assert.False(context.ShouldCacheResponse);

            middleware.FinalizeCachingHeaders(context);

            Assert.True(context.ShouldCacheResponse);
        }

        [Fact]
        public void FinalizeCachingHeaders_DefaultResponseValidity_Is10Seconds()
        {
            var middleware = TestUtils.CreateTestMiddleware();
            var context = TestUtils.CreateTestContext();

            middleware.FinalizeCachingHeaders(context);

            Assert.Equal(TimeSpan.FromSeconds(10), context.CachedResponseValidFor);
        }

        [Fact]
        public void FinalizeCachingHeaders_ResponseValidity_UseExpiryIfAvailable()
        {
            var utcNow = DateTimeOffset.MinValue;
            var middleware = TestUtils.CreateTestMiddleware();
            var context = TestUtils.CreateTestContext();

            context.ResponseTime = utcNow;
            context.TypedResponseHeaders.Expires = utcNow + TimeSpan.FromSeconds(11);

            middleware.FinalizeCachingHeaders(context);

            Assert.Equal(TimeSpan.FromSeconds(11), context.CachedResponseValidFor);
        }

        [Fact]
        public void FinalizeCachingHeaders_ResponseValidity_UseMaxAgeIfAvailable()
        {
            var middleware = TestUtils.CreateTestMiddleware();
            var context = TestUtils.CreateTestContext();
            context.TypedResponseHeaders.CacheControl = new CacheControlHeaderValue()
            {
                MaxAge = TimeSpan.FromSeconds(12)
            };

            context.ResponseTime = DateTimeOffset.UtcNow;
            context.TypedResponseHeaders.Expires = context.ResponseTime + TimeSpan.FromSeconds(11);

            middleware.FinalizeCachingHeaders(context);

            Assert.Equal(TimeSpan.FromSeconds(12), context.CachedResponseValidFor);
        }

        [Fact]
        public void FinalizeCachingHeaders_ResponseValidity_UseSharedMaxAgeIfAvailable()
        {
            var middleware = TestUtils.CreateTestMiddleware();
            var context = TestUtils.CreateTestContext();
            context.TypedResponseHeaders.CacheControl = new CacheControlHeaderValue()
            {
                MaxAge = TimeSpan.FromSeconds(12),
                SharedMaxAge = TimeSpan.FromSeconds(13)
            };

            context.ResponseTime = DateTimeOffset.UtcNow;
            context.TypedResponseHeaders.Expires = context.ResponseTime + TimeSpan.FromSeconds(11);

            middleware.FinalizeCachingHeaders(context);

            Assert.Equal(TimeSpan.FromSeconds(13), context.CachedResponseValidFor);
        }

        [Fact]
        public void FinalizeCachingHeaders_UpdateCachedVaryRules_IfNotEquivalentToPrevious()
        {
            var cache = new TestResponseCache();
            var middleware = TestUtils.CreateTestMiddleware(cache);
            var context = TestUtils.CreateTestContext();

            context.HttpContext.Response.Headers[HeaderNames.Vary] = new StringValues(new[] { "headerA", "HEADERB", "HEADERc" });
            context.HttpContext.AddResponseCachingFeature();
            context.HttpContext.GetResponseCachingFeature().VaryParams = new StringValues(new[] { "paramB", "PARAMAA" });
            var cachedVaryRules = new CachedVaryRules()
            {
                Headers = new StringValues(new[] { "HeaderA", "HeaderB" }),
                Params = new StringValues(new[] { "ParamA", "ParamB" })
            };
            context.CachedVaryRules = cachedVaryRules;

            middleware.FinalizeCachingHeaders(context);

            Assert.Equal(1, cache.SetCount);
            Assert.NotSame(cachedVaryRules, context.CachedVaryRules);
        }

        [Fact]
        public void FinalizeCachingHeaders_DoNotUpdateCachedVaryRules_IfEquivalentToPrevious()
        {
            var cache = new TestResponseCache();
            var middleware = TestUtils.CreateTestMiddleware(cache);
            var context = TestUtils.CreateTestContext();

            context.HttpContext.Response.Headers[HeaderNames.Vary] = new StringValues(new[] { "headerA", "HEADERB" });
            context.HttpContext.AddResponseCachingFeature();
            context.HttpContext.GetResponseCachingFeature().VaryParams = new StringValues(new[] { "paramB", "PARAMA" });
            var cachedVaryRules = new CachedVaryRules()
            {
                VaryKeyPrefix = FastGuid.NewGuid().IdString,
                Headers = new StringValues(new[] { "HEADERA", "HEADERB" }),
                Params = new StringValues(new[] { "PARAMA", "PARAMB" })
            };
            context.CachedVaryRules = cachedVaryRules;

            middleware.FinalizeCachingHeaders(context);

            Assert.Equal(0, cache.SetCount);
            Assert.Same(cachedVaryRules, context.CachedVaryRules);
        }

        [Fact]
        public void FinalizeCachingHeaders_DoNotAddDate_IfSpecified()
        {
            var utcNow = DateTimeOffset.MinValue;
            var middleware = TestUtils.CreateTestMiddleware();
            var context = TestUtils.CreateTestContext();
            context.ResponseTime = utcNow;

            Assert.Null(context.TypedResponseHeaders.Date);

            middleware.FinalizeCachingHeaders(context);

            Assert.Equal(utcNow, context.TypedResponseHeaders.Date);
        }

        [Fact]
        public void FinalizeCachingHeaders_AddsDate_IfNoneSpecified()
        {
            var utcNow = DateTimeOffset.MinValue;
            var middleware = TestUtils.CreateTestMiddleware();
            var context = TestUtils.CreateTestContext();
            context.TypedResponseHeaders.Date = utcNow;
            context.ResponseTime = utcNow + TimeSpan.FromSeconds(10);

            Assert.Equal(utcNow, context.TypedResponseHeaders.Date);

            middleware.FinalizeCachingHeaders(context);

            Assert.Equal(utcNow, context.TypedResponseHeaders.Date);
        }

        [Fact]
        public void FinalizeCachingHeaders_StoresCachedResponse_InState()
        {
            var middleware = TestUtils.CreateTestMiddleware();
            var context = TestUtils.CreateTestContext();

            Assert.Null(context.CachedResponse);

            middleware.FinalizeCachingHeaders(context);

            Assert.NotNull(context.CachedResponse);
        }

        [Fact]
        public async Task FinalizeCachingBody_StoreResponseBodySeparately_IfLargerThanLimit()
        {
            var cache = new TestResponseCache();
            var middleware = TestUtils.CreateTestMiddleware(cache);
            var context = TestUtils.CreateTestContext();

            middleware.ShimResponseStream(context);
            await context.HttpContext.Response.WriteAsync(new string('0', 70 * 1024));

            context.ShouldCacheResponse = true;
            context.CachedResponse = new CachedResponse()
            {
                BodyKeyPrefix = FastGuid.NewGuid().IdString
            };
            context.StorageBaseKey = "BaseKey";
            context.CachedResponseValidFor = TimeSpan.FromSeconds(10);

            middleware.FinalizeCachingBody(context);

            Assert.Equal(2, cache.SetCount);
        }

        [Fact]
        public async Task FinalizeCachingBody_StoreResponseBodyInCachedResponse_IfSmallerThanLimit()
        {
            var cache = new TestResponseCache();
            var middleware = TestUtils.CreateTestMiddleware(cache);
            var context = TestUtils.CreateTestContext();

            middleware.ShimResponseStream(context);
            await context.HttpContext.Response.WriteAsync(new string('0', 70 * 1024 - 1));

            context.ShouldCacheResponse = true;
            context.CachedResponse = new CachedResponse()
            {
                BodyKeyPrefix = FastGuid.NewGuid().IdString
            };
            context.StorageBaseKey = "BaseKey";
            context.CachedResponseValidFor = TimeSpan.FromSeconds(10);

            middleware.FinalizeCachingBody(context);

            Assert.Equal(1, cache.SetCount);
        }

        [Fact]
        public async Task FinalizeCachingBody_StoreResponseBodySeparately_LimitIsConfigurable()
        {
            var cache = new TestResponseCache();
            var middleware = TestUtils.CreateTestMiddleware(cache, new ResponseCachingOptions()
            {
                MinimumSplitBodySize = 2048
            });
            var context = TestUtils.CreateTestContext();

            middleware.ShimResponseStream(context);
            await context.HttpContext.Response.WriteAsync(new string('0', 1024));

            context.ShouldCacheResponse = true;
            context.CachedResponse = new CachedResponse()
            {
                BodyKeyPrefix = FastGuid.NewGuid().IdString
            };
            context.StorageBaseKey = "BaseKey";
            context.CachedResponseValidFor = TimeSpan.FromSeconds(10);

            middleware.FinalizeCachingBody(context);

            Assert.Equal(1, cache.SetCount);
        }

        [Fact]
        public async Task FinalizeCachingBody_Cache_IfContentLengthMatches()
        {
            var cache = new TestResponseCache();
            var middleware = TestUtils.CreateTestMiddleware(cache);
            var context = TestUtils.CreateTestContext();

            middleware.ShimResponseStream(context);
            context.HttpContext.Response.ContentLength = 10;
            await context.HttpContext.Response.WriteAsync(new string('0', 10));

            context.ShouldCacheResponse = true;
            context.CachedResponse = new CachedResponse()
            {
                BodyKeyPrefix = FastGuid.NewGuid().IdString
            };
            context.StorageBaseKey = "BaseKey";
            context.CachedResponseValidFor = TimeSpan.FromSeconds(10);

            middleware.FinalizeCachingBody(context);

            Assert.Equal(1, cache.SetCount);
        }

        [Fact]
        public async Task FinalizeCachingBody_DoNotCache_IfContentLengthMismatches()
        {
            var cache = new TestResponseCache();
            var middleware = TestUtils.CreateTestMiddleware(cache);
            var context = TestUtils.CreateTestContext();

            middleware.ShimResponseStream(context);
            context.HttpContext.Response.ContentLength = 9;
            await context.HttpContext.Response.WriteAsync(new string('0', 10));

            context.ShouldCacheResponse = true;
            context.CachedResponse = new CachedResponse()
            {
                BodyKeyPrefix = FastGuid.NewGuid().IdString
            };
            context.StorageBaseKey = "BaseKey";
            context.CachedResponseValidFor = TimeSpan.FromSeconds(10);

            middleware.FinalizeCachingBody(context);

            Assert.Equal(0, cache.SetCount);
        }

        [Fact]
        public async Task FinalizeCachingBody_Cache_IfContentLengthAbsent()
        {
            var cache = new TestResponseCache();
            var middleware = TestUtils.CreateTestMiddleware(cache);
            var context = TestUtils.CreateTestContext();

            middleware.ShimResponseStream(context);
            await context.HttpContext.Response.WriteAsync(new string('0', 10));

            context.ShouldCacheResponse = true;
            context.CachedResponse = new CachedResponse()
            {
                BodyKeyPrefix = FastGuid.NewGuid().IdString
            };
            context.StorageBaseKey = "BaseKey";
            context.CachedResponseValidFor = TimeSpan.FromSeconds(10);

            middleware.FinalizeCachingBody(context);

            Assert.Equal(1, cache.SetCount);
        }

        [Fact]
        public void NormalizeStringValues_NormalizesCasingToUpper()
        {
            var uppercaseStrings = new StringValues(new[] { "STRINGA", "STRINGB" });
            var lowercaseStrings = new StringValues(new[] { "stringA", "stringB" });

            var normalizedStrings = ResponseCachingMiddleware.GetNormalizedStringValues(lowercaseStrings);

            Assert.Equal(uppercaseStrings, normalizedStrings);
        }

        [Fact]
        public void NormalizeStringValues_NormalizesOrder()
        {
            var orderedStrings = new StringValues(new[] { "STRINGA", "STRINGB" });
            var reverseOrderStrings = new StringValues(new[] { "STRINGB", "STRINGA" });

            var normalizedStrings = ResponseCachingMiddleware.GetNormalizedStringValues(reverseOrderStrings);

            Assert.Equal(orderedStrings, normalizedStrings);
        }
    }
}
