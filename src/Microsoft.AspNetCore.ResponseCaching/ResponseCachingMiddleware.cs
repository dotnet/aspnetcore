// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.ResponseCaching
{
    // http://tools.ietf.org/html/rfc7234
    public class ResponseCachingMiddleware
    {
        private static readonly Func<object, Task> OnStartingCallback = state =>
        {
            ((ResponseCachingContext)state).OnResponseStarting();
            return Task.FromResult(0);
        };

        private readonly RequestDelegate _next;
        private readonly IResponseCache _cache;

        public ResponseCachingMiddleware(RequestDelegate next, IResponseCache cache)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            _next = next;
            _cache = cache;
        }

        public async Task Invoke(HttpContext context)
        {
            var cachingContext = new ResponseCachingContext(context, _cache);

            // Should we attempt any caching logic?
            if (cachingContext.RequestIsCacheable())
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
    }
}
