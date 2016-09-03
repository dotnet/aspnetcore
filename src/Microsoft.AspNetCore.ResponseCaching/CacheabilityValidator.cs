// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.ResponseCaching.Internal;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.ResponseCaching
{
    public class CacheabilityValidator : ICacheabilityValidator
    {
        public virtual bool RequestIsCacheable(HttpContext httpContext)
        {
            var state = httpContext.GetResponseCachingState();

            // Verify the method
            // TODO: RFC lists POST as a cacheable method when explicit freshness information is provided, but this is not widely implemented. Will revisit.
            var request = httpContext.Request;
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
                if (state.RequestCacheControl.NoCache)
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

        public virtual bool ResponseIsCacheable(HttpContext httpContext)
        {
            var state = httpContext.GetResponseCachingState();

            // Only cache pages explicitly marked with public
            // TODO: Consider caching responses that are not marked as public but otherwise cacheable?
            if (!state.ResponseCacheControl.Public)
            {
                return false;
            }

            // Check no-store
            if (state.RequestCacheControl.NoStore || state.ResponseCacheControl.NoStore)
            {
                return false;
            }

            // Check no-cache
            // TODO: Handle no-cache with headers
            if (state.ResponseCacheControl.NoCache)
            {
                return false;
            }

            var response = httpContext.Response;

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
            if (state.ResponseCacheControl.Private)
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
            if (state.ResponseHeaders.Date == null)
            {
                if (state.ResponseCacheControl.SharedMaxAge == null &&
                    state.ResponseCacheControl.MaxAge == null &&
                    state.ResponseTime > state.ResponseHeaders.Expires)
                {
                    return false;
                }
            }
            else
            {
                var age = state.ResponseTime - state.ResponseHeaders.Date.Value;

                // Validate shared max age
                if (age > state.ResponseCacheControl.SharedMaxAge)
                {
                    return false;
                }
                else if (state.ResponseCacheControl.SharedMaxAge == null)
                {
                    // Validate max age
                    if (age > state.ResponseCacheControl.MaxAge)
                    {
                        return false;
                    }
                    else if (state.ResponseCacheControl.MaxAge == null)
                    {
                        // Validate expiration
                        if (state.ResponseTime > state.ResponseHeaders.Expires)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public virtual bool CachedEntryIsFresh(HttpContext httpContext, ResponseHeaders cachedResponseHeaders)
        {
            var state = httpContext.GetResponseCachingState();
            var age = state.CachedEntryAge;

            // Add min-fresh requirements
            if (state.RequestCacheControl.MinFresh != null)
            {
                age += state.RequestCacheControl.MinFresh.Value;
            }

            // Validate shared max age, this overrides any max age settings for shared caches
            if (age > cachedResponseHeaders.CacheControl.SharedMaxAge)
            {
                // shared max age implies must revalidate
                return false;
            }
            else if (cachedResponseHeaders.CacheControl.SharedMaxAge == null)
            {
                // Validate max age
                if (age > cachedResponseHeaders.CacheControl.MaxAge || age > state.RequestCacheControl.MaxAge)
                {
                    // Must revalidate
                    if (cachedResponseHeaders.CacheControl.MustRevalidate)
                    {
                        return false;
                    }

                    // Request allows stale values
                    if (age < state.RequestCacheControl.MaxStaleLimit)
                    {
                        // TODO: Add warning header indicating the response is stale
                        return true;
                    }

                    return false;
                }
                else if (cachedResponseHeaders.CacheControl.MaxAge == null && state.RequestCacheControl.MaxAge == null)
                {
                    // Validate expiration
                    if (state.ResponseTime > cachedResponseHeaders.Expires)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
