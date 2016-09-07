// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.ResponseCaching.Internal;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Xunit;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.ResponseCaching.Tests
{
    public class ResponseCachingContextTests
    {

        [Fact]
        public void ConditionalRequestSatisfied_NotConditionalRequest_Fails()
        {
            var context = CreateTestContext(new DefaultHttpContext());
            var cachedHeaders = new ResponseHeaders(new HeaderDictionary());

            Assert.False(context.ConditionalRequestSatisfied(cachedHeaders));
        }

        [Fact]
        public void ConditionalRequestSatisfied_IfUnmodifiedSince_FallsbackToDateHeader()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var cachedHeaders = new ResponseHeaders(new HeaderDictionary());
            var httpContext = new DefaultHttpContext();
            var context = CreateTestContext(httpContext);

            httpContext.Request.GetTypedHeaders().IfUnmodifiedSince = utcNow;

            // Verify modifications in the past succeeds
            cachedHeaders.Date = utcNow - TimeSpan.FromSeconds(10);
            Assert.True(context.ConditionalRequestSatisfied(cachedHeaders));

            // Verify modifications at present succeeds
            cachedHeaders.Date = utcNow;
            Assert.True(context.ConditionalRequestSatisfied(cachedHeaders));

            // Verify modifications in the future fails
            cachedHeaders.Date = utcNow + TimeSpan.FromSeconds(10);
            Assert.False(context.ConditionalRequestSatisfied(cachedHeaders));
        }

        [Fact]
        public void ConditionalRequestSatisfied_IfUnmodifiedSince_LastModifiedOverridesDateHeader()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var cachedHeaders = new ResponseHeaders(new HeaderDictionary());
            var httpContext = new DefaultHttpContext();
            var context = CreateTestContext(httpContext);

            httpContext.Request.GetTypedHeaders().IfUnmodifiedSince = utcNow;

            // Verify modifications in the past succeeds
            cachedHeaders.Date = utcNow + TimeSpan.FromSeconds(10);
            cachedHeaders.LastModified = utcNow - TimeSpan.FromSeconds(10);
            Assert.True(context.ConditionalRequestSatisfied(cachedHeaders));

            // Verify modifications at present
            cachedHeaders.Date = utcNow + TimeSpan.FromSeconds(10);
            cachedHeaders.LastModified = utcNow;
            Assert.True(context.ConditionalRequestSatisfied(cachedHeaders));

            // Verify modifications in the future fails
            cachedHeaders.Date = utcNow - TimeSpan.FromSeconds(10);
            cachedHeaders.LastModified = utcNow + TimeSpan.FromSeconds(10);
            Assert.False(context.ConditionalRequestSatisfied(cachedHeaders));
        }

        [Fact]
        public void ConditionalRequestSatisfied_IfNoneMatch_Overrides_IfUnmodifiedSince_ToPass()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var cachedHeaders = new ResponseHeaders(new HeaderDictionary());
            var httpContext = new DefaultHttpContext();
            var requestHeaders = httpContext.Request.GetTypedHeaders();
            var context = CreateTestContext(httpContext);

            // This would fail the IfUnmodifiedSince checks
            requestHeaders.IfUnmodifiedSince = utcNow;
            cachedHeaders.LastModified = utcNow + TimeSpan.FromSeconds(10);

            requestHeaders.IfNoneMatch = new List<EntityTagHeaderValue>(new[] { EntityTagHeaderValue.Any });
            Assert.True(context.ConditionalRequestSatisfied(cachedHeaders));
        }

        [Fact]
        public void ConditionalRequestSatisfied_IfNoneMatch_Overrides_IfUnmodifiedSince_ToFail()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var cachedHeaders = new ResponseHeaders(new HeaderDictionary());
            var httpContext = new DefaultHttpContext();
            var requestHeaders = httpContext.Request.GetTypedHeaders();
            var context = CreateTestContext(httpContext);

            // This would pass the IfUnmodifiedSince checks
            requestHeaders.IfUnmodifiedSince = utcNow;
            cachedHeaders.LastModified = utcNow - TimeSpan.FromSeconds(10);

            requestHeaders.IfNoneMatch = new List<EntityTagHeaderValue>(new[] { new EntityTagHeaderValue("\"E1\"") });
            Assert.False(context.ConditionalRequestSatisfied(cachedHeaders));
        }

        [Fact]
        public void ConditionalRequestSatisfied_IfNoneMatch_AnyWithoutETagInResponse_Passes()
        {
            var cachedHeaders = new ResponseHeaders(new HeaderDictionary());
            var httpContext = new DefaultHttpContext();
            var context = CreateTestContext(httpContext);

            httpContext.Request.GetTypedHeaders().IfNoneMatch = new List<EntityTagHeaderValue>(new[] { new EntityTagHeaderValue("\"E1\"") });

            Assert.False(context.ConditionalRequestSatisfied(cachedHeaders));
        }

        [Fact]
        public void ConditionalRequestSatisfied_IfNoneMatch_ExplicitWithMatch_Passes()
        {
            var cachedHeaders = new ResponseHeaders(new HeaderDictionary())
            {
                ETag = new EntityTagHeaderValue("\"E1\"")
            };
            var httpContext = new DefaultHttpContext();
            var context = CreateTestContext(httpContext);

            httpContext.Request.GetTypedHeaders().IfNoneMatch = new List<EntityTagHeaderValue>(new[] { new EntityTagHeaderValue("\"E1\"") });

            Assert.True(context.ConditionalRequestSatisfied(cachedHeaders));
        }

        [Fact]
        public void ConditionalRequestSatisfied_IfNoneMatch_ExplicitWithoutMatch_Fails()
        {
            var cachedHeaders = new ResponseHeaders(new HeaderDictionary())
            {
                ETag = new EntityTagHeaderValue("\"E2\"")
            };
            var httpContext = new DefaultHttpContext();
            var context = CreateTestContext(httpContext);

            httpContext.Request.GetTypedHeaders().IfNoneMatch = new List<EntityTagHeaderValue>(new[] { new EntityTagHeaderValue("\"E1\"") });

            Assert.False(context.ConditionalRequestSatisfied(cachedHeaders));
        }

        [Fact]
        public void FinalizeCachingHeaders_DoNotUpdateShouldCacheResponse_IfResponseIsNotCacheable()
        {
            var httpContext = new DefaultHttpContext();
            var context = CreateTestContext(httpContext);
            var state = httpContext.GetResponseCachingState();

            Assert.False(state.ShouldCacheResponse);

            context.ShimResponseStream();
            context.FinalizeCachingHeaders();

            Assert.False(state.ShouldCacheResponse);
        }

        [Fact]
        public void FinalizeCachingHeaders_UpdateShouldCacheResponse_IfResponseIsCacheable()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
            {
                Public = true
            };
            var context = CreateTestContext(httpContext);
            var state = httpContext.GetResponseCachingState();

            Assert.False(state.ShouldCacheResponse);

            context.FinalizeCachingHeaders();

            Assert.True(state.ShouldCacheResponse);
        }

        [Fact]
        public void FinalizeCachingHeaders_DefaultResponseValidity_Is10Seconds()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
            {
                Public = true
            };
            var context = CreateTestContext(httpContext);

            context.FinalizeCachingHeaders();

            Assert.Equal(TimeSpan.FromSeconds(10), httpContext.GetResponseCachingState().CachedResponseValidFor);
        }

        [Fact]
        public void FinalizeCachingHeaders_ResponseValidity_UseExpiryIfAvailable()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
            {
                Public = true
            };
            var context = CreateTestContext(httpContext);

            var state = httpContext.GetResponseCachingState();
            var utcNow = DateTimeOffset.MinValue;
            state.ResponseTime = utcNow;
            state.ResponseHeaders.Expires = utcNow + TimeSpan.FromSeconds(11);

            context.FinalizeCachingHeaders();

            Assert.Equal(TimeSpan.FromSeconds(11), state.CachedResponseValidFor);
        }

        [Fact]
        public void FinalizeCachingHeaders_ResponseValidity_UseMaxAgeIfAvailable()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(12)
            };
            var context = CreateTestContext(httpContext);

            var state = httpContext.GetResponseCachingState();
            state.ResponseTime = DateTimeOffset.UtcNow;
            state.ResponseHeaders.Expires = state.ResponseTime + TimeSpan.FromSeconds(11);

            context.FinalizeCachingHeaders();

            Assert.Equal(TimeSpan.FromSeconds(12), state.CachedResponseValidFor);
        }

        [Fact]
        public void FinalizeCachingHeaders_ResponseValidity_UseSharedMaxAgeIfAvailable()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(12),
                SharedMaxAge = TimeSpan.FromSeconds(13)
            };
            var context = CreateTestContext(httpContext);

            var state = httpContext.GetResponseCachingState();
            state.ResponseTime = DateTimeOffset.UtcNow;
            state.ResponseHeaders.Expires = state.ResponseTime + TimeSpan.FromSeconds(11);

            context.FinalizeCachingHeaders();

            Assert.Equal(TimeSpan.FromSeconds(13), state.CachedResponseValidFor);
        }

        [Fact]
        public void FinalizeCachingHeaders_UpdateCachedVaryRules_IfNotEquivalentToPrevious()
        {
            var httpContext = new DefaultHttpContext();
            var cache = new TestResponseCache();
            var context = CreateTestContext(httpContext, cache, new ResponseCachingOptions());
            var state = httpContext.GetResponseCachingState();

            httpContext.Response.Headers[HeaderNames.Vary] = new StringValues(new[] { "headerA", "HEADERB", "HEADERc" });
            httpContext.AddResponseCachingFeature();
            httpContext.GetResponseCachingFeature().VaryParams = new StringValues(new[] { "paramB", "PARAMAA" });
            httpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
            };
            var cachedVaryRules = new CachedVaryRules()
            {
                VaryRules = new VaryRules()
                {
                    Headers = new StringValues(new[] { "HeaderA", "HeaderB" }),
                    Params = new StringValues(new[] { "ParamA", "ParamB" })
                }
            };
            state.CachedVaryRules = cachedVaryRules;

            context.FinalizeCachingHeaders();

            Assert.Equal(1, cache.StoredItems);
            Assert.NotSame(cachedVaryRules, state.CachedVaryRules);
        }

        [Fact]
        public void FinalizeCachingHeaders_DoNotUpdateCachedVaryRules_IfEquivalentToPrevious()
        {
            var httpContext = new DefaultHttpContext();
            var cache = new TestResponseCache();
            var context = CreateTestContext(httpContext, cache, new ResponseCachingOptions());
            var state = httpContext.GetResponseCachingState();

            httpContext.Response.Headers[HeaderNames.Vary] = new StringValues(new[] { "headerA", "HEADERB" });
            httpContext.AddResponseCachingFeature();
            httpContext.GetResponseCachingFeature().VaryParams = new StringValues(new[] { "paramB", "PARAMA" });
            httpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
            };
            var cachedVaryRules = new CachedVaryRules()
            {
                VaryKeyPrefix = FastGuid.NewGuid().IdString,
                VaryRules = new VaryRules()
                {
                    Headers = new StringValues(new[] { "HEADERA", "HEADERB" }),
                    Params = new StringValues(new[] { "PARAMA", "PARAMB" })
                }
            };
            state.CachedVaryRules = cachedVaryRules;

            context.FinalizeCachingHeaders();

            Assert.Equal(0, cache.StoredItems);
            Assert.Same(cachedVaryRules, state.CachedVaryRules);
        }

        [Fact]
        public void FinalizeCachingHeaders_DoNotAddDate_IfSpecified()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
            {
                Public = true
            };
            var context = CreateTestContext(httpContext);
            var state = httpContext.GetResponseCachingState();
            var utcNow = DateTimeOffset.MinValue;
            state.ResponseTime = utcNow;

            Assert.Null(state.ResponseHeaders.Date);

            context.FinalizeCachingHeaders();

            Assert.Equal(utcNow, state.ResponseHeaders.Date);
        }

        [Fact]
        public void FinalizeCachingHeaders_AddsDate_IfNoneSpecified()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
            {
                Public = true
            };
            var context = CreateTestContext(httpContext);
            var state = httpContext.GetResponseCachingState();
            var utcNow = DateTimeOffset.MinValue;
            state.ResponseHeaders.Date = utcNow;
            state.ResponseTime = utcNow + TimeSpan.FromSeconds(10);

            Assert.Equal(utcNow, state.ResponseHeaders.Date);

            context.FinalizeCachingHeaders();

            Assert.Equal(utcNow, state.ResponseHeaders.Date);
        }

        [Fact]
        public void FinalizeCachingHeaders_StoresCachedResponse_InState()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
            {
                Public = true
            };
            var context = CreateTestContext(httpContext);
            var state = httpContext.GetResponseCachingState();

            Assert.Null(state.CachedResponse);

            context.FinalizeCachingHeaders();

            Assert.NotNull(state.CachedResponse);
        }

        [Fact]
        public async Task FinalizeCachingBody_StoreResponseBodySeparately_IfLargerThanLimit()
        {
            var httpContext = new DefaultHttpContext();
            var cache = new TestResponseCache();
            var context = CreateTestContext(httpContext, cache, new ResponseCachingOptions());

            context.ShimResponseStream();
            await httpContext.Response.WriteAsync(new string('0', 70 * 1024));

            var state = httpContext.GetResponseCachingState();
            state.ShouldCacheResponse = true;
            state.CachedResponse = new CachedResponse()
            {
                BodyKeyPrefix = FastGuid.NewGuid().IdString
            };
            state.BaseKey = "BaseKey";
            state.CachedResponseValidFor = TimeSpan.FromSeconds(10);

            context.FinalizeCachingBody();

            Assert.Equal(2, cache.StoredItems);
        }

        [Fact]
        public async Task FinalizeCachingBody_StoreResponseBodyInCachedResponse_IfSmallerThanLimit()
        {
            var httpContext = new DefaultHttpContext();
            var cache = new TestResponseCache();
            var context = CreateTestContext(httpContext, cache, new ResponseCachingOptions());

            context.ShimResponseStream();
            await httpContext.Response.WriteAsync(new string('0', 70 * 1024 - 1));

            var state = httpContext.GetResponseCachingState();
            state.ShouldCacheResponse = true;
            state.CachedResponse = new CachedResponse()
            {
                BodyKeyPrefix = FastGuid.NewGuid().IdString
            };
            state.BaseKey = "BaseKey";
            state.CachedResponseValidFor = TimeSpan.FromSeconds(10);

            context.FinalizeCachingBody();

            Assert.Equal(1, cache.StoredItems);
        }

        [Fact]
        public void NormalizeStringValues_NormalizesCasingToUpper()
        {
            var uppercaseStrings = new StringValues(new[] { "STRINGA", "STRINGB" });
            var lowercaseStrings = new StringValues(new[] { "stringA", "stringB" });

            var normalizedStrings = ResponseCachingContext.GetNormalizedStringValues(lowercaseStrings);

            Assert.Equal(uppercaseStrings, normalizedStrings);
        }

        [Fact]
        public void NormalizeStringValues_NormalizesOrder()
        {
            var orderedStrings = new StringValues(new[] { "STRINGA", "STRINGB" });
            var reverseOrderStrings = new StringValues(new[] { "STRINGB", "STRINGA" });

            var normalizedStrings = ResponseCachingContext.GetNormalizedStringValues(reverseOrderStrings);

            Assert.Equal(orderedStrings, normalizedStrings);
        }

        private static ResponseCachingContext CreateTestContext(HttpContext httpContext)
        {
            return CreateTestContext(
                httpContext,
                new TestResponseCache(),
                new ResponseCachingOptions(),
                new CacheabilityValidator());
        }

        private static ResponseCachingContext CreateTestContext(HttpContext httpContext, ResponseCachingOptions options)
        {
            return CreateTestContext(
                httpContext,
                new TestResponseCache(),
                options,
                new CacheabilityValidator());
        }

        private static ResponseCachingContext CreateTestContext(HttpContext httpContext, ICacheabilityValidator cacheabilityValidator)
        {
            return CreateTestContext(
                httpContext,
                new TestResponseCache(),
                new ResponseCachingOptions(),
                cacheabilityValidator);
        }

        private static ResponseCachingContext CreateTestContext(HttpContext httpContext, IResponseCache responseCache, ResponseCachingOptions options)
        {
            return CreateTestContext(
                httpContext,
                responseCache,
                options,
                new CacheabilityValidator());
        }

        private static ResponseCachingContext CreateTestContext(
            HttpContext httpContext,
            IResponseCache responseCache,
            ResponseCachingOptions options,
            ICacheabilityValidator cacheabilityValidator)
        {
            httpContext.AddResponseCachingState();

            return new ResponseCachingContext(
                httpContext,
                responseCache,
                options,
                cacheabilityValidator,
                new KeyProvider(new DefaultObjectPoolProvider(), Options.Create(options)));
        }

        private class TestResponseCache : IResponseCache
        {
            public int StoredItems { get; private set; }

            public object Get(string key)
            {
                return null;
            }

            public void Remove(string key)
            {
            }

            public void Set(string key, object entry, TimeSpan validFor)
            {
                StoredItems++;
            }
        }

        private class TestHttpSendFileFeature : IHttpSendFileFeature
        {
            public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellation)
            {
                return Task.FromResult(0);
            }
        }
    }
}
