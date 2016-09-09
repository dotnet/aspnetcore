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
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.ResponseCaching.Tests
{
    public class ResponseCachingContextTests
    {
        [Fact]
        public async Task TryServeFromCacheAsync_OnlyIfCached_Serves504()
        {
            var cache = new TestResponseCache();
            var httpContext = new DefaultHttpContext();
            var context = CreateTestContext(httpContext, responseCache: cache, keyProvider: new TestKeyProvider());
            httpContext.Request.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
            {
                OnlyIfCached = true
            };

            Assert.True(await context.TryServeFromCacheAsync());
            Assert.Equal(StatusCodes.Status504GatewayTimeout, httpContext.Response.StatusCode);
        }

        [Fact]
        public async Task TryServeFromCacheAsync_CachedResponseNotFound_Fails()
        {
            var cache = new TestResponseCache();
            var httpContext = new DefaultHttpContext();
            var context = CreateTestContext(httpContext, responseCache: cache, keyProvider: new TestKeyProvider(new[] { "BaseKey", "BaseKey2" }));

            Assert.False(await context.TryServeFromCacheAsync());
            Assert.Equal(2, cache.GetCount);
        }

        [Fact]
        public async Task TryServeFromCacheAsync_CachedResponseFound_Succeeds()
        {
            var cache = new TestResponseCache();
            var httpContext = new DefaultHttpContext();
            var context = CreateTestContext(httpContext, responseCache: cache, keyProvider: new TestKeyProvider(new[] { "BaseKey", "BaseKey2" }));

            cache.Set(
                "BaseKey2",
                new CachedResponse()
                {
                    Body = new byte[0]
                },
                TimeSpan.Zero);

            Assert.True(await context.TryServeFromCacheAsync());
            Assert.Equal(2, cache.GetCount);
        }

        [Fact]
        public async Task TryServeFromCacheAsync_VaryRuleFound_CachedResponseNotFound_Fails()
        {
            var cache = new TestResponseCache();
            var httpContext = new DefaultHttpContext();
            var context = CreateTestContext(httpContext, responseCache: cache, keyProvider: new TestKeyProvider(new[] { "BaseKey", "BaseKey2" }));

            cache.Set(
                "BaseKey2",
                new CachedVaryRules(),
                TimeSpan.Zero);

            Assert.False(await context.TryServeFromCacheAsync());
            Assert.Equal(2, cache.GetCount);
        }

        [Fact]
        public async Task TryServeFromCacheAsync_VaryRuleFound_CachedResponseFound_Succeeds()
        {
            var cache = new TestResponseCache();
            var httpContext = new DefaultHttpContext();
            var context = CreateTestContext(httpContext, responseCache: cache, keyProvider: new TestKeyProvider(new[] { "BaseKey", "BaseKey2" }, new[] { "VaryKey", "VaryKey2" }));

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

            Assert.True(await context.TryServeFromCacheAsync());
            Assert.Equal(6, cache.GetCount);
        }

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
            var context = CreateTestContext(httpContext, cacheabilityValidator: new CacheabilityValidator());
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
            var context = CreateTestContext(httpContext, cacheabilityValidator: new CacheabilityValidator());
            var state = httpContext.GetResponseCachingState();

            Assert.False(state.ShouldCacheResponse);

            context.FinalizeCachingHeaders();

            Assert.True(state.ShouldCacheResponse);
        }

        [Fact]
        public void FinalizeCachingHeaders_DefaultResponseValidity_Is10Seconds()
        {
            var httpContext = new DefaultHttpContext();
            var context = CreateTestContext(httpContext);

            context.FinalizeCachingHeaders();

            Assert.Equal(TimeSpan.FromSeconds(10), httpContext.GetResponseCachingState().CachedResponseValidFor);
        }

        [Fact]
        public void FinalizeCachingHeaders_ResponseValidity_UseExpiryIfAvailable()
        {
            var httpContext = new DefaultHttpContext();
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
            var context = CreateTestContext(httpContext, cache);
            var state = httpContext.GetResponseCachingState();

            httpContext.Response.Headers[HeaderNames.Vary] = new StringValues(new[] { "headerA", "HEADERB", "HEADERc" });
            httpContext.AddResponseCachingFeature();
            httpContext.GetResponseCachingFeature().VaryParams = new StringValues(new[] { "paramB", "PARAMAA" });
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

            Assert.Equal(1, cache.SetCount);
            Assert.NotSame(cachedVaryRules, state.CachedVaryRules);
        }

        [Fact]
        public void FinalizeCachingHeaders_DoNotUpdateCachedVaryRules_IfEquivalentToPrevious()
        {
            var httpContext = new DefaultHttpContext();
            var cache = new TestResponseCache();
            var context = CreateTestContext(httpContext, cache);
            var state = httpContext.GetResponseCachingState();

            httpContext.Response.Headers[HeaderNames.Vary] = new StringValues(new[] { "headerA", "HEADERB" });
            httpContext.AddResponseCachingFeature();
            httpContext.GetResponseCachingFeature().VaryParams = new StringValues(new[] { "paramB", "PARAMA" });
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

            Assert.Equal(0, cache.SetCount);
            Assert.Same(cachedVaryRules, state.CachedVaryRules);
        }

        [Fact]
        public void FinalizeCachingHeaders_DoNotAddDate_IfSpecified()
        {
            var httpContext = new DefaultHttpContext();
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
            var context = CreateTestContext(httpContext, cache);

            context.ShimResponseStream();
            await httpContext.Response.WriteAsync(new string('0', 70 * 1024));

            var state = httpContext.GetResponseCachingState();
            state.ShouldCacheResponse = true;
            state.CachedResponse = new CachedResponse()
            {
                BodyKeyPrefix = FastGuid.NewGuid().IdString
            };
            state.StorageBaseKey = "BaseKey";
            state.CachedResponseValidFor = TimeSpan.FromSeconds(10);

            context.FinalizeCachingBody();

            Assert.Equal(2, cache.SetCount);
        }

        [Fact]
        public async Task FinalizeCachingBody_StoreResponseBodyInCachedResponse_IfSmallerThanLimit()
        {
            var httpContext = new DefaultHttpContext();
            var cache = new TestResponseCache();
            var context = CreateTestContext(httpContext, cache);

            context.ShimResponseStream();
            await httpContext.Response.WriteAsync(new string('0', 70 * 1024 - 1));

            var state = httpContext.GetResponseCachingState();
            state.ShouldCacheResponse = true;
            state.CachedResponse = new CachedResponse()
            {
                BodyKeyPrefix = FastGuid.NewGuid().IdString
            };
            state.StorageBaseKey = "BaseKey";
            state.CachedResponseValidFor = TimeSpan.FromSeconds(10);

            context.FinalizeCachingBody();

            Assert.Equal(1, cache.SetCount);
        }

        [Fact]
        public async Task FinalizeCachingBody_StoreResponseBodySeparately_LimitIsConfigurable()
        {
            var httpContext = new DefaultHttpContext();
            var cache = new TestResponseCache();
            var context = CreateTestContext(httpContext, cache, new ResponseCachingOptions()
            {
                MinimumSplitBodySize = 2048
            });

            context.ShimResponseStream();
            await httpContext.Response.WriteAsync(new string('0', 1024));

            var state = httpContext.GetResponseCachingState();
            state.ShouldCacheResponse = true;
            state.CachedResponse = new CachedResponse()
            {
                BodyKeyPrefix = FastGuid.NewGuid().IdString
            };
            state.StorageBaseKey = "BaseKey";
            state.CachedResponseValidFor = TimeSpan.FromSeconds(10);

            context.FinalizeCachingBody();

            Assert.Equal(1, cache.SetCount);
        }

        [Fact]
        public async Task FinalizeCachingBody_Cache_IfContentLengthMatches()
        {
            var httpContext = new DefaultHttpContext();
            var cache = new TestResponseCache();
            var context = CreateTestContext(httpContext, cache);

            context.ShimResponseStream();
            httpContext.Response.ContentLength = 10;
            await httpContext.Response.WriteAsync(new string('0', 10));

            var state = httpContext.GetResponseCachingState();
            state.ShouldCacheResponse = true;
            state.CachedResponse = new CachedResponse()
            {
                BodyKeyPrefix = FastGuid.NewGuid().IdString
            };
            state.StorageBaseKey = "BaseKey";
            state.CachedResponseValidFor = TimeSpan.FromSeconds(10);

            context.FinalizeCachingBody();

            Assert.Equal(1, cache.SetCount);
        }

        [Fact]
        public async Task FinalizeCachingBody_DoNotCache_IfContentLengthMismatches()
        {
            var httpContext = new DefaultHttpContext();
            var cache = new TestResponseCache();
            var context = CreateTestContext(httpContext, cache);

            context.ShimResponseStream();
            httpContext.Response.ContentLength = 9;
            await httpContext.Response.WriteAsync(new string('0', 10));

            var state = httpContext.GetResponseCachingState();
            state.ShouldCacheResponse = true;
            state.CachedResponse = new CachedResponse()
            {
                BodyKeyPrefix = FastGuid.NewGuid().IdString
            };
            state.StorageBaseKey = "BaseKey";
            state.CachedResponseValidFor = TimeSpan.FromSeconds(10);

            context.FinalizeCachingBody();

            Assert.Equal(0, cache.SetCount);
        }

        [Fact]
        public async Task FinalizeCachingBody_Cache_IfContentLengthAbsent()
        {
            var httpContext = new DefaultHttpContext();
            var cache = new TestResponseCache();
            var context = CreateTestContext(httpContext, cache);

            context.ShimResponseStream();
            await httpContext.Response.WriteAsync(new string('0', 10));

            var state = httpContext.GetResponseCachingState();
            state.ShouldCacheResponse = true;
            state.CachedResponse = new CachedResponse()
            {
                BodyKeyPrefix = FastGuid.NewGuid().IdString
            };
            state.StorageBaseKey = "BaseKey";
            state.CachedResponseValidFor = TimeSpan.FromSeconds(10);

            context.FinalizeCachingBody();

            Assert.Equal(1, cache.SetCount);
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

        private static ResponseCachingContext CreateTestContext(
            HttpContext httpContext,
            IResponseCache responseCache = null,
            ResponseCachingOptions options = null,
            IKeyProvider keyProvider = null,
            ICacheabilityValidator cacheabilityValidator = null)
        {
            if (responseCache == null)
            {
                responseCache = new TestResponseCache();
            }
            if (options == null)
            {
                options = new ResponseCachingOptions();
            }
            if (keyProvider == null)
            {
                keyProvider = new KeyProvider(new DefaultObjectPoolProvider(), Options.Create(options));
            }
            if (cacheabilityValidator == null)
            {
                cacheabilityValidator = new TestCacheabilityValidator();
            }

            httpContext.AddResponseCachingState();

            return new ResponseCachingContext(
                httpContext,
                responseCache,
                options,
                cacheabilityValidator,
                keyProvider);
        }

        private class TestCacheabilityValidator : ICacheabilityValidator
        {
            public bool CachedEntryIsFresh(HttpContext httpContext, ResponseHeaders cachedResponseHeaders) => true;

            public bool RequestIsCacheable(HttpContext httpContext) => true;

            public bool ResponseIsCacheable(HttpContext httpContext) => true;
        }

        private class TestKeyProvider : IKeyProvider
        {
            private readonly StringValues _baseKey;
            private readonly StringValues _varyKey;

            public TestKeyProvider(StringValues? lookupBaseKey = null, StringValues? lookupVaryKey = null)
            {
                if (lookupBaseKey.HasValue)
                {
                    _baseKey = lookupBaseKey.Value;
                }
                if (lookupVaryKey.HasValue)
                {
                    _varyKey = lookupVaryKey.Value;
                }
            }

            public IEnumerable<string> CreateLookupBaseKey(HttpContext httpContext) => _baseKey;


            public IEnumerable<string> CreateLookupVaryKey(HttpContext httpContext, VaryRules varyRules)
            {
                foreach (var baseKey in _baseKey)
                {
                    foreach (var varyKey in _varyKey)
                    {
                        yield return baseKey + varyKey;
                    }
                }
            }

            public string CreateBodyKey(HttpContext httpContext)
            {
                throw new NotImplementedException();
            }

            public string CreateStorageBaseKey(HttpContext httpContext)
            {
                throw new NotImplementedException();
            }

            public string CreateStorageVaryKey(HttpContext httpContext, VaryRules varyRules)
            {
                throw new NotImplementedException();
            }
        }

        private class TestResponseCache : IResponseCache
        {
            private readonly IDictionary<string, object> _storage = new Dictionary<string, object>();
            public int GetCount { get; private set; }
            public int SetCount { get; private set; }

            public object Get(string key)
            {
                GetCount++;
                try
                {
                    return _storage[key];
                }
                catch
                {
                    return null;
                }
            }

            public void Remove(string key)
            {
            }

            public void Set(string key, object entry, TimeSpan validFor)
            {
                SetCount++;
                _storage[key] = entry;
            }
        }

        private class TestHttpSendFileFeature : IHttpSendFileFeature
        {
            public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellation)
            {
                return TaskCache.CompletedTask;
            }
        }
    }
}
