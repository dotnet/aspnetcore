// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.ResponseCaching.Tests
{
    internal class TestUtils
    {
        internal static RequestDelegate DefaultRequestDelegate = async (context) =>
        {
            var uniqueId = Guid.NewGuid().ToString();
            var headers = context.Response.GetTypedHeaders();
            headers.CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromSeconds(10)
            };
            headers.Date = DateTimeOffset.UtcNow;
            headers.Headers["X-Value"] = uniqueId;
            await context.Response.WriteAsync(uniqueId);
        };

        internal static ICacheKeyProvider CreateTestKeyProvider()
        {
            return CreateTestKeyProvider(new ResponseCachingOptions());
        }

        internal static ICacheKeyProvider CreateTestKeyProvider(ResponseCachingOptions options)
        {
            return new CacheKeyProvider(new DefaultObjectPoolProvider(), Options.Create(options));
        }

        internal static IWebHostBuilder CreateBuilderWithResponseCaching(
            Action<IApplicationBuilder> configureDelegate = null,
            ResponseCachingOptions options = null,
            RequestDelegate requestDelegate = null)
        {
            if (configureDelegate == null)
            {
                configureDelegate = app => { };
            }
            if (options == null)
            {
                options = new ResponseCachingOptions();
            }
            if (requestDelegate == null)
            {
                requestDelegate = DefaultRequestDelegate;
            }

            return new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddDistributedResponseCache();
                })
                .Configure(app =>
                {
                    configureDelegate(app);
                    app.UseResponseCaching(options);
                    app.Run(requestDelegate);
                });
        }

        internal static ResponseCachingMiddleware CreateTestMiddleware(
            IResponseCache responseCache = null,
            ResponseCachingOptions options = null,
            ICacheKeyProvider cacheKeyProvider = null,
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
            if (cacheKeyProvider == null)
            {
                cacheKeyProvider = new CacheKeyProvider(new DefaultObjectPoolProvider(), Options.Create(options));
            }
            if (cacheabilityValidator == null)
            {
                cacheabilityValidator = new TestCacheabilityValidator();
            }

            return new ResponseCachingMiddleware(
                httpContext => TaskCache.CompletedTask,
                responseCache,
                Options.Create(options),
                cacheabilityValidator,
                cacheKeyProvider);
        }

        internal static ResponseCachingContext CreateTestContext()
        {
            return new ResponseCachingContext(new DefaultHttpContext());
        }
    }

    internal class DummySendFileFeature : IHttpSendFileFeature
    {
        public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellation)
        {
            return TaskCache.CompletedTask;
        }
    }

    internal class TestCacheabilityValidator : ICacheabilityValidator
    {
        public bool IsCachedEntryFresh(ResponseCachingContext context) => true;

        public bool IsRequestCacheable(ResponseCachingContext context) => true;

        public bool IsResponseCacheable(ResponseCachingContext context) => true;
    }

    internal class TestKeyProvider : ICacheKeyProvider
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

        public IEnumerable<string> CreateLookupBaseKeys(ResponseCachingContext context) => _baseKey;


        public IEnumerable<string> CreateLookupVaryKeys(ResponseCachingContext context)
        {
            foreach (var baseKey in _baseKey)
            {
                foreach (var varyKey in _varyKey)
                {
                    yield return baseKey + varyKey;
                }
            }
        }

        public string CreateStorageBaseKey(ResponseCachingContext context)
        {
            throw new NotImplementedException();
        }

        public string CreateStorageVaryKey(ResponseCachingContext context)
        {
            throw new NotImplementedException();
        }
    }

    internal class TestResponseCache : IResponseCache
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

    internal class TestHttpSendFileFeature : IHttpSendFileFeature
    {
        public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellation)
        {
            return TaskCache.CompletedTask;
        }
    }
}
