// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Caching.Memory;

namespace Microsoft.AspNet.ResponseCaching
{
    internal class CachingContext
    {
        private string _cacheKey;

        public CachingContext(HttpContext httpContext, IMemoryCache cache)
        {
            HttpContext = httpContext;
            Cache = cache;
        }

        private HttpContext HttpContext { get; }

        private IMemoryCache Cache { get; }

        private Stream OriginalResponseStream { get; set; }

        private MemoryStream Buffer { get; set; }

        internal bool ResponseStarted { get; set; }

        private bool CacheResponse { get; set; }

        internal bool CheckRequestAllowsCaching()
        {
            // Verify the method
            // TODO: What other methods should be supported?
            if (!string.Equals("GET", HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Verify the request headers do not opt-out of caching
            // TODO:
            return true;
        }

        // Only QueryString is treated as case sensitive
        // GET;HTTP://MYDOMAIN.COM:80/PATHBASE/PATH?QueryString
        private string CreateCacheKey()
        {
            var request = HttpContext.Request;
            return request.Method.ToUpperInvariant()
                + ";"
                + request.Scheme.ToUpperInvariant()
                + "://"
                + request.Host.Value.ToUpperInvariant()
                + request.PathBase.Value.ToUpperInvariant()
                + request.Path.Value.ToUpperInvariant()
                + request.QueryString;
        }

        internal async Task<bool> TryServeFromCacheAsync()
        {
            _cacheKey = CreateCacheKey();
            ResponseCacheEntry cacheEntry;
            if (Cache.TryGetValue(_cacheKey, out cacheEntry))
            {
                // TODO: Compare cached request headers

                // TODO: Evaluate Vary-By and select the most appropriate response

                // TODO: Content negotiation if there are multiple cached response formats?

                // TODO: Verify content freshness, or else re-validate the data?

                var response = HttpContext.Response;
                // Copy the cached status code and response headers
                response.StatusCode = cacheEntry.StatusCode;
                foreach (var pair in cacheEntry.Headers)
                {
                    response.Headers.SetValues(pair.Key, pair.Value);
                }

                // TODO: Update cache headers (Age)
                response.Headers["Served_From_Cache"] = DateTime.Now.ToString();

                // Copy the cached response body
                var body = cacheEntry.Body;
                if (body.Length > 0)
                {
                    await response.Body.WriteAsync(body, 0, body.Length);
                }
                return true;
            }

            return false;
        }

        internal void HookResponseStream()
        {
            // TODO: Use a wrapper stream to listen for writes (e.g. the start of the response),
            // check the headers, and verify if we should cache the response.
            // Then we should stream data out to the client at the same time as we buffer for the cache.
            // For now we'll just buffer everything in memory before checking the response headers.
            // TODO: Consider caching large responses on disk and serving them from there.
            OriginalResponseStream = HttpContext.Response.Body;
            Buffer = new MemoryStream();
            HttpContext.Response.Body = Buffer;
        }

        internal bool OnResponseStarting()
        {
            // Evaluate the response headers, see if we should buffer and cache
            CacheResponse = true; // TODO:
            return CacheResponse;
        }

        internal void FinalizeCaching()
        {
            if (CacheResponse)
            {
                // Store the buffer to cache
                var cacheEntry = new ResponseCacheEntry();
                cacheEntry.StatusCode = HttpContext.Response.StatusCode;
                cacheEntry.Headers = HttpContext.Response.Headers.ToList();
                cacheEntry.Body = Buffer.ToArray();
                Cache.Set(_cacheKey, cacheEntry); // TODO: Timeouts
            }

            // TODO: TEMP, flush the buffer to the client
            Buffer.Seek(0, SeekOrigin.Begin);
            Buffer.CopyTo(OriginalResponseStream);
        }

        internal void UnhookResponseStream()
        {
            // Unhook the response stream.
            HttpContext.Response.Body = OriginalResponseStream;
        }
    }
}
