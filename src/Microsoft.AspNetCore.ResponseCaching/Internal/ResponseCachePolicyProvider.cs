// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.ResponseCaching.Internal
{
    public class ResponseCachePolicyProvider : IResponseCachePolicyProvider
    {
        private static readonly CacheControlHeaderValue EmptyCacheControl = new CacheControlHeaderValue();

        public virtual bool IsRequestCacheable(ResponseCacheContext context)
        {
            // Verify the method
            var request = context.HttpContext.Request;
            if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
            {
                return false;
            }

            // Verify existence of authorization headers
            if (!StringValues.IsNullOrEmpty(request.Headers[HeaderNames.Authorization]))
            {
                return false;
            }

            // Verify request cache-control parameters
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

            return true;
        }

        public virtual bool IsResponseCacheable(ResponseCacheContext context)
        {
            // Only cache pages explicitly marked with public
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

            // Check private
            if (context.ResponseCacheControlHeaderValue.Private)
            {
                return false;
            }

            // Check response code
            if (response.StatusCode != StatusCodes.Status200OK)
            {
                return false;
            }

            // Check response freshness
            if (!context.ResponseDate.HasValue)
            {
                if (!context.ResponseCacheControlHeaderValue.SharedMaxAge.HasValue &&
                    !context.ResponseCacheControlHeaderValue.MaxAge.HasValue &&
                    context.ResponseTime.Value >= context.ResponseExpires)
                {
                    return false;
                }
            }
            else
            {
                var age = context.ResponseTime.Value - context.ResponseDate.Value;

                // Validate shared max age
                if (age >= context.ResponseCacheControlHeaderValue.SharedMaxAge)
                {
                    return false;
                }
                else if (!context.ResponseCacheControlHeaderValue.SharedMaxAge.HasValue)
                {
                    // Validate max age
                    if (age >= context.ResponseCacheControlHeaderValue.MaxAge)
                    {
                        return false;
                    }
                    else if (!context.ResponseCacheControlHeaderValue.MaxAge.HasValue)
                    {
                        // Validate expiration
                        if (context.ResponseTime.Value >= context.ResponseExpires)
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
            var age = context.CachedEntryAge.Value;
            var cachedControlHeaders = context.CachedResponseHeaders.CacheControl ?? EmptyCacheControl;

            // Add min-fresh requirements
            if (context.RequestCacheControlHeaderValue.MinFresh.HasValue)
            {
                age += context.RequestCacheControlHeaderValue.MinFresh.Value;
            }

            // Validate shared max age, this overrides any max age settings for shared caches
            if (age >= cachedControlHeaders.SharedMaxAge)
            {
                // shared max age implies must revalidate
                return false;
            }
            else if (!cachedControlHeaders.SharedMaxAge.HasValue)
            {
                // Validate max age
                if (age >= cachedControlHeaders.MaxAge || age >= context.RequestCacheControlHeaderValue.MaxAge)
                {
                    // Must revalidate
                    if (cachedControlHeaders.MustRevalidate)
                    {
                        return false;
                    }

                    // Request allows stale values
                    if (age < context.RequestCacheControlHeaderValue.MaxStaleLimit)
                    {
                        return true;
                    }

                    return false;
                }
                else if (!cachedControlHeaders.MaxAge.HasValue && !context.RequestCacheControlHeaderValue.MaxAge.HasValue)
                {
                    // Validate expiration
                    if (context.ResponseTime.Value >= context.CachedResponseHeaders.Expires)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
