// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCaching.Internal;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.ResponseCaching
{
    public class ResponseCachingContext
    {
        private string _cacheKey;
        private RequestType _requestType;

        public ResponseCachingContext(HttpContext httpContext, IResponseCache cache)
        {
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }

            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            HttpContext = httpContext;
            Cache = cache;
        }

        private HttpContext HttpContext { get; }

        private IResponseCache Cache { get; }

        private Stream OriginalResponseStream { get; set; }

        private MemoryStream Buffer { get; set; }

        internal bool ResponseStarted { get; set; }

        private bool CacheResponse { get; set; }

        private bool IsProxied { get; set; }

        public bool CheckRequestAllowsCaching()
        {
            // Verify the method
            // TODO: What other methods should be supported?
            if (string.Equals("GET", HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                _requestType = RequestType.FullReponse;
            }
            else if (string.Equals("HEAD", HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals("OPTIONS", HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                _requestType = RequestType.HeadersOnly;
            }
            else
            {
                _requestType = RequestType.NotCached;
                return false;
            }

            // Verify the request headers do not opt-out of caching
            // TODO:
            return true;
        }

        // Only QueryString is treated as case sensitive
        // GET;/PATH;VaryBy
        private string CreateCacheKey()
        {
            return CreateCacheKey(varyBy: null);
        }

        private string CreateCacheKey(CachedVaryBy varyBy)
        {
            var request = HttpContext.Request;
            var builder = new StringBuilder()
                .Append(request.Method.ToUpperInvariant())
                .Append(";")
                .Append(request.Path.Value.ToUpperInvariant())
                .Append(CreateVaryByCacheKey(varyBy));

            return builder.ToString();
        }

        private string CreateVaryByCacheKey(CachedVaryBy varyBy)
        {
            // TODO: resolve key format and delimiters
            if (varyBy == null)
            {
                return string.Empty;
            }

            var request = HttpContext.Request;
            var builder = new StringBuilder(";");

            foreach (var header in varyBy.Headers)
            {
                var value = request.Headers[header].ToString();
                // null vs Empty?
                if (string.IsNullOrEmpty(value))
                {
                    value = "null";
                }

                builder.Append(header)
                    .Append("=")
                    .Append(value)
                    .Append(";");
            }

            // Parse querystring params

            return builder.ToString();
        }

        internal async Task<bool> TryServeFromCacheAsync()
        {
            _cacheKey = CreateCacheKey();
            var cacheEntry = Cache.Get(_cacheKey);

            if (cacheEntry is CachedVaryBy)
            {
                // Request contains VaryBy rules, recompute key and try again
                _cacheKey = CreateCacheKey(cacheEntry as CachedVaryBy);
                cacheEntry = Cache.Get(_cacheKey);
            }

            if (cacheEntry is CachedResponse)
            {
                // TODO: Compare cached request headers

                // TODO: Content negotiation if there are multiple cached response formats?

                // TODO: Verify content freshness, or else re-validate the data?

                var cachedResponse = cacheEntry as CachedResponse;

                var response = HttpContext.Response;
                // Copy the cached status code and response headers
                response.StatusCode = cachedResponse.StatusCode;
                foreach (var pair in cachedResponse.Headers)
                {
                    response.Headers[pair.Key] = pair.Value;
                }

                // TODO: Allow setting proxied _isProxied 
                var age = Math.Max((DateTimeOffset.UtcNow - cacheEntry.Created).TotalSeconds, 0.0);
                var ageString = (age > int.MaxValue ? int.MaxValue : (int)age).ToString(CultureInfo.InvariantCulture);
                response.Headers[IsProxied ? "Age" : "X-Cache-Age"] = ageString;

                if (_requestType == RequestType.HeadersOnly)
                {
                    response.Headers["Content-Length"] = "0";
                }
                else
                {
                    // Copy the cached response body
                    var body = cachedResponse.Body;
                    response.Headers["Content-Length"] = body.Length.ToString(CultureInfo.InvariantCulture);
                    if (body.Length > 0)
                    {
                        await response.Body.WriteAsync(body, 0, body.Length);
                    }
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
            // Don't cache errors? 404 etc
            if (CacheResponse && HttpContext.Response.StatusCode == 200)
            {
                var response = HttpContext.Response;
                var varyHeaderValue = response.Headers["Vary"];

                // Check if any VaryBy rules exist
                if (!StringValues.IsNullOrEmpty(varyHeaderValue))
                {
                    var cachedVaryBy = new CachedVaryBy
                    {
                        // Only vary by headers for now
                        Headers = varyHeaderValue
                    };

                    Cache.Set(_cacheKey, cachedVaryBy);
                    _cacheKey = CreateCacheKey(cachedVaryBy);
                }

                // Store the response to cache
                var cachedResponse = new CachedResponse
                {
                    Created = DateTimeOffset.UtcNow,
                    StatusCode = HttpContext.Response.StatusCode,
                    Body = Buffer.ToArray()
                };

                var headers = HttpContext.Response.Headers;
                var count = headers.Count
                    - (headers.ContainsKey("Date") ? 1 : 0)
                    - (headers.ContainsKey("Content-Length") ? 1 : 0)
                    - (headers.ContainsKey("Age") ? 1 : 0);
                var cachedHeaders = new List<KeyValuePair<string, StringValues>>(count);
                var age = 0;
                foreach (var entry in headers)
                {
                    // Reduce create date by Age 
                    if (entry.Key == "Age" && int.TryParse(entry.Value, out age) && age > 0)
                    {
                        cacheEntry.Created -= new TimeSpan(0, 0, age);
                    }
                    // Don't copy Date header or Content-Length
                    else if (entry.Key != "Date" && entry.Key != "Content-Length")
                    {
                        cachedHeaders.Add(entry);
                    }
                }

                Cache.Set(_cacheKey, cachedResponse); // TODO: Timeouts
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

        private enum RequestType
        {
            NotCached = 0,
            HeadersOnly,
            FullReponse
        }
    }
}
