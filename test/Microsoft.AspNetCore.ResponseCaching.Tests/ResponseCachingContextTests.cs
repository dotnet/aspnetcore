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

        private static ResponseCachingContext CreateTestContext(HttpContext httpContext)
        {
            return CreateTestContext(
                httpContext,
                new ResponseCachingOptions(),
                new CacheabilityValidator());
        }

        private static ResponseCachingContext CreateTestContext(HttpContext httpContext, ResponseCachingOptions options)
        {
            return CreateTestContext(
                httpContext,
                options,
                new CacheabilityValidator());
        }

        private static ResponseCachingContext CreateTestContext(HttpContext httpContext, ICacheabilityValidator cacheabilityValidator)
        {
            return CreateTestContext(
                httpContext,
                new ResponseCachingOptions(),
                cacheabilityValidator);
        }

        private static ResponseCachingContext CreateTestContext(
            HttpContext httpContext,
            ResponseCachingOptions options,
            ICacheabilityValidator cacheabilityValidator)
        {
            httpContext.AddResponseCachingState();

            return new ResponseCachingContext(
                httpContext,
                new TestResponseCache(),
                options,
                cacheabilityValidator,
                new KeyProvider(new DefaultObjectPoolProvider(), Options.Create(options)));
        }

        private class TestResponseCache : IResponseCache
        {
            public object Get(string key)
            {
                return null;
            }

            public void Remove(string key)
            {
            }

            public void Set(string key, object entry, TimeSpan validFor)
            {
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
