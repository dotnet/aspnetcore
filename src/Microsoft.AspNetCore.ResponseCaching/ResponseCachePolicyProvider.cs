// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.ResponseCaching
{
    public class ResponseCachePolicyProvider : IResponseCachePolicyProvider
    {
        private static readonly CacheControlHeaderValue EmptyCacheControl = new CacheControlHeaderValue();

        public virtual bool IsRequestCacheable(ResponseCacheContext context)
        {
            // Verify the method
            // TODO: RFC lists POST as a cacheable method when explicit freshness information is provided, but this is not widely implemented. Will revisit.
            var request = context.HttpContext.Request;
            if (!string.Equals("GET", request.Method, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals("HEAD", request.Method, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Verify existence of authorization headers
            // TODO: The server may indicate that the response to these request are cacheable
            if (!StringValues.IsNullOrEmpty(request.Headers[HeaderNames.Authorization]))
            {
                return false;
            }

            // Verify request cache-control parameters
            // TODO: no-cache requests can be retrieved upon validation with origin
            if (!StringValues.IsNullOrEmpty(request.Headers[HeaderNames.CacheControl]))
            {
                if (context.RequestCacheControlHeaderValue.NoCache)
                {
                    return false;
                }
            }
            else
            {
                // Support for legacy HTTP 1.0 cache directive
                var pragmaHeaderValues = request.Headers[HeaderNames.Pragma];
                foreach (var directive in pragmaHeaderValues)
                {
                    if (string.Equals("no-cache", directive, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            // TODO: Verify global middleware settings? Explicit ignore list, range requests, etc.
            return true;
        }

        public virtual bool IsResponseCacheable(ResponseCacheContext context)
        {
            // Only cache pages explicitly marked with public
            // TODO: Consider caching responses that are not marked as public but otherwise cacheable?
            if (!context.ResponseCacheControlHeaderValue.Public)
            {
                return false;
            }

            // Check no-store
            if (context.RequestCacheControlHeaderValue.NoStore || context.ResponseCacheControlHeaderValue.NoStore)
            {
                return false;
            }

            // Check no-cache
            // TODO: Handle no-cache with headers
            if (context.ResponseCacheControlHeaderValue.NoCache)
            {
                return false;
            }

            var response = context.HttpContext.Response;

            // Do not cache responses with Set-Cookie headers
            if (!StringValues.IsNullOrEmpty(response.Headers[HeaderNames.SetCookie]))
            {
                return false;
            }

            // Do not cache responses varying by *
            var varyHeader = response.Headers[HeaderNames.Vary];
            if (varyHeader.Count == 1 && string.Equals(varyHeader, "*", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // TODO: public MAY override the cacheability checks for private and status codes

            // Check private
            if (context.ResponseCacheControlHeaderValue.Private)
            {
                return false;
            }

            // Check response code
            // TODO: RFC also lists 203, 204, 206, 300, 301, 404, 405, 410, 414, and 501 as cacheable by default
            if (response.StatusCode != StatusCodes.Status200OK)
            {
                return false;
            }

            // Check response freshness
            // TODO: apparent age vs corrected age value
            if (context.TypedResponseHeaders.Date == null)
            {
                if (context.ResponseCacheControlHeaderValue.SharedMaxAge == null &&
                    context.ResponseCacheControlHeaderValue.MaxAge == null &&
                    context.ResponseTime > context.TypedResponseHeaders.Expires)
                {
                    return false;
                }
            }
            else
            {
                var age = context.ResponseTime - context.TypedResponseHeaders.Date.Value;

                // Validate shared max age
                if (age > context.ResponseCacheControlHeaderValue.SharedMaxAge)
                {
                    return false;
                }
                else if (context.ResponseCacheControlHeaderValue.SharedMaxAge == null)
                {
                    // Validate max age
                    if (age > context.ResponseCacheControlHeaderValue.MaxAge)
                    {
                        return false;
                    }
                    else if (context.ResponseCacheControlHeaderValue.MaxAge == null)
                    {
                        // Validate expiration
                        if (context.ResponseTime > context.TypedResponseHeaders.Expires)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public virtual bool IsCachedEntryFresh(ResponseCacheContext context)
        {
            var age = context.CachedEntryAge;
            var cachedControlHeaders = context.CachedResponseHeaders.CacheControl ?? EmptyCacheControl;

            // Add min-fresh requirements
            if (context.RequestCacheControlHeaderValue.MinFresh != null)
            {
                age += context.RequestCacheControlHeaderValue.MinFresh.Value;
            }

            // Validate shared max age, this overrides any max age settings for shared caches
            if (age > cachedControlHeaders.SharedMaxAge)
            {
                // shared max age implies must revalidate
                return false;
            }
            else if (cachedControlHeaders.SharedMaxAge == null)
            {
                // Validate max age
                if (age > cachedControlHeaders.MaxAge || age > context.RequestCacheControlHeaderValue.MaxAge)
                {
                    // Must revalidate
                    if (cachedControlHeaders.MustRevalidate)
                    {
                        return false;
                    }

                    // Request allows stale values
                    if (age < context.RequestCacheControlHeaderValue.MaxStaleLimit)
                    {
                        // TODO: Add warning header indicating the response is stale
                        return true;
                    }

                    return false;
                }
                else if (cachedControlHeaders.MaxAge == null && context.RequestCacheControlHeaderValue.MaxAge == null)
                {
                    // Validate expiration
                    if (context.ResponseTime > context.CachedResponseHeaders.Expires)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
