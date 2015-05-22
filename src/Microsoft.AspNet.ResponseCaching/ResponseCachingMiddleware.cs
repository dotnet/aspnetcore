// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Caching.Memory;

namespace Microsoft.AspNet.ResponseCaching
{
    // http://tools.ietf.org/html/rfc7234
    public class ResponseCachingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;

        public ResponseCachingMiddleware(RequestDelegate next, IMemoryCache cache)
        {
            _next = next;
            _cache = cache;
        }

        public async Task Invoke(HttpContext context)
        {
            var cachingContext = new CachingContext(context, _cache);
            // Should we attempt any caching logic?
            if (cachingContext.CheckRequestAllowsCaching())
            {
                // Can this request be served from cache?
                if (await cachingContext.TryServeFromCacheAsync())
                {
                    return;
                }

                // Hook up to listen to the response stream
                cachingContext.HookResponseStream();

                try
                {
                    await _next(context);

                    // If there was no response body, check the response headers now. We can cache things like redirects.
                    if (!cachingContext.ResponseStarted)
                    {
                        cachingContext.OnResponseStarting();
                    }
                    // Finalize the cache entry
                    cachingContext.FinalizeCaching();
                }
                finally
                {
                    cachingContext.UnhookResponseStream();
                }
            }
            else
            {
                await _next(context);
            }
        }
    }
}
