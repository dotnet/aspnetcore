// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCaching.Internal;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ResponseCaching
{
    public class ResponseCachingMiddleware
    {
        private static readonly Func<object, Task> OnStartingCallback = state =>
        {
            ((ResponseCachingContext)state).OnResponseStarting();
            return TaskCache.CompletedTask;
        };

        private readonly RequestDelegate _next;
        private readonly IResponseCache _cache;
        private readonly ResponseCachingOptions _options;
        private readonly ICacheabilityValidator _cacheabilityValidator;
        private readonly IKeyProvider _keyProvider;

        public ResponseCachingMiddleware(
            RequestDelegate next,
            IResponseCache cache,
            IOptions<ResponseCachingOptions> options,
            ICacheabilityValidator cacheabilityValidator,
            IKeyProvider keyProvider)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (cacheabilityValidator == null)
            {
                throw new ArgumentNullException(nameof(cacheabilityValidator));
            }
            if (keyProvider == null)
            {
                throw new ArgumentNullException(nameof(keyProvider));
            }

            _next = next;
            _cache = cache;
            _options = options.Value;
            _cacheabilityValidator = cacheabilityValidator;
            _keyProvider = keyProvider;
        }

        public async Task Invoke(HttpContext context)
        {
            context.AddResponseCachingState();

            try
            {
                var cachingContext = new ResponseCachingContext(
                    context,
                    _cache,
                    _options,
                    _cacheabilityValidator,
                    _keyProvider);

                // Should we attempt any caching logic?
                if (_cacheabilityValidator.RequestIsCacheable(context))
                {
                    // Can this request be served from cache?
                    if (await cachingContext.TryServeFromCacheAsync())
                    {
                        return;
                    }

                    // Hook up to listen to the response stream
                    cachingContext.ShimResponseStream();

                    try
                    {
                        // Subscribe to OnStarting event
                        context.Response.OnStarting(OnStartingCallback, cachingContext);

                        await _next(context);

                        // If there was no response body, check the response headers now. We can cache things like redirects.
                        cachingContext.OnResponseStarting();

                        // Finalize the cache entry
                        cachingContext.FinalizeCachingBody();
                    }
                    finally
                    {
                        cachingContext.UnshimResponseStream();
                    }
                }
                else
                {
                    // TODO: Invalidate resources for successful unsafe methods? Required by RFC
                    await _next(context);
                }
            }
            finally
            {
                context.RemoveResponseCachingState();
            }
        }
    }
}
