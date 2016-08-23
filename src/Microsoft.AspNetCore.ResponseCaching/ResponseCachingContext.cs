// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.ResponseCaching.Internal;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.ResponseCaching
{
    public class ResponseCachingContext
    {
        private static readonly CacheControlHeaderValue EmptyCacheControl = new CacheControlHeaderValue();
        private string _cacheKey;
        private ResponseType? _responseType;
        private RequestHeaders _requestHeaders;
        private ResponseHeaders _responseHeaders;
        private CacheControlHeaderValue _requestCacheControl;
        private CacheControlHeaderValue _responseCacheControl;
        private bool? _cacheResponse;
        private CachedResponse _cachedResponse;
        private TimeSpan _cachedResponseValidFor;
        internal DateTimeOffset _responseTime;
        
        public ResponseCachingContext(
            HttpContext httpContext,
            IResponseCache cache,
            IResponseCachingCacheabilityValidator cacheabilityValidator,
            IResponseCachingCacheKeySuffixProvider cacheKeySuffixProvider)
            : this(httpContext, cache, new SystemClock(), cacheabilityValidator, cacheKeySuffixProvider)
        {
        }

        // Internal for testing
        internal ResponseCachingContext(
            HttpContext httpContext, 
            IResponseCache cache,
            ISystemClock clock,
            IResponseCachingCacheabilityValidator cacheabilityValidator,
            IResponseCachingCacheKeySuffixProvider cacheKeySuffixProvider)
        {
            HttpContext = httpContext;
            Cache = cache;
            Clock = clock;
            CacheabilityValidator = cacheabilityValidator;
            CacheKeySuffixProvider = cacheKeySuffixProvider;
        }

        internal bool CacheResponse
        {
            get
            {
                if (_cacheResponse == null)
                {
                    // TODO: apparent age vs corrected age value
                    var responseAge = _responseTime - ResponseHeaders.Date ?? TimeSpan.Zero;

                    _cacheResponse = ResponseIsCacheable() && EntryIsFresh(ResponseHeaders, responseAge, verifyAgainstRequest: false);
                }
                return _cacheResponse.Value;
            }
        }

        internal bool ResponseStarted { get; set; }

        private HttpContext HttpContext { get; }

        private IResponseCache Cache { get; }

        private ISystemClock Clock { get; }

        private IResponseCachingCacheabilityValidator CacheabilityValidator { get; }

        private IResponseCachingCacheKeySuffixProvider CacheKeySuffixProvider { get; }

        private Stream OriginalResponseStream { get; set; }

        private ResponseCacheStream ResponseCacheStream { get; set; }

        private IHttpSendFileFeature OriginalSendFileFeature { get; set; }

        private RequestHeaders RequestHeaders
        {
            get
            {
                if (_requestHeaders == null)
                {
                    _requestHeaders = HttpContext.Request.GetTypedHeaders();
                }
                return _requestHeaders;
            }
        }

        private ResponseHeaders ResponseHeaders
        {
            get
            {
                if (_responseHeaders == null)
                {
                    _responseHeaders = HttpContext.Response.GetTypedHeaders();
                }
                return _responseHeaders;
            }
        }

        private CacheControlHeaderValue RequestCacheControl
        {
            get
            {
                if (_requestCacheControl == null)
                {
                    _requestCacheControl = RequestHeaders.CacheControl ?? EmptyCacheControl;
                }
                return _requestCacheControl;
            }
        }

        private CacheControlHeaderValue ResponseCacheControl
        {
            get
            {
                if (_responseCacheControl == null)
                {
                    _responseCacheControl = ResponseHeaders.CacheControl ?? EmptyCacheControl;
                }
                return _responseCacheControl;
            }
        }

        // GET;/PATH;VaryBy
        // TODO: Method invariant retrieval? E.g. HEAD after GET to the same resource.
        internal string CreateCacheKey()
        {
            return CreateCacheKey(varyBy: null);
        }

        internal string CreateCacheKey(CachedVaryBy varyBy)
        {
            var request = HttpContext.Request;
            var builder = new StringBuilder()
                .Append(request.Method.ToUpperInvariant())
                .Append(";")
                .Append(request.Path.Value.ToUpperInvariant());

            if (varyBy?.Headers.Count > 0)
            {
                // TODO: resolve key format and delimiters
                foreach (var header in varyBy.Headers)
                {
                    // TODO: Normalization of order, case?
                    var value = HttpContext.Request.Headers[header];

                    // TODO: How to handle null/empty string?
                    if (StringValues.IsNullOrEmpty(value))
                    {
                        value = "null";
                    }

                    builder.Append(";")
                        .Append(header)
                        .Append("=")
                        .Append(value);
                }
            }
            // TODO: Parse querystring params

            // Append custom cache key segment
            var customKey = CacheKeySuffixProvider.CreateCustomKeySuffix(HttpContext);
            if (!string.IsNullOrEmpty(customKey))
            {
                builder.Append(";")
                    .Append(customKey);
            }

            return builder.ToString();
        }

        internal bool RequestIsCacheable()
        {
            // Use optional override if specified by user
            switch(CacheabilityValidator.RequestIsCacheableOverride(HttpContext))
            {
                case OverrideResult.UseDefaultLogic:
                    break;
                case OverrideResult.DoNotCache:
                    return false;
                case OverrideResult.Cache:
                    return true;
                default:
                    throw new NotSupportedException($"Unrecognized result from {nameof(CacheabilityValidator.RequestIsCacheableOverride)}.");
            }

            // Verify the method
            // TODO: RFC lists POST as a cacheable method when explicit freshness information is provided, but this is not widely implemented. Will revisit.
            var request = HttpContext.Request;
            if (string.Equals("GET", request.Method, StringComparison.OrdinalIgnoreCase))
            {
                _responseType = ResponseType.FullReponse;
            }
            else if (string.Equals("HEAD", request.Method, StringComparison.OrdinalIgnoreCase))
            {
                _responseType = ResponseType.HeadersOnly;
            }
            else
            {
                return false;
            }

            // Verify existence of authorization headers
            // TODO: The server may indicate that the response to these request are cacheable
            if (!string.IsNullOrEmpty(request.Headers[HeaderNames.Authorization]))
            {
                return false;
            }

            // Verify request cache-control parameters
            // TODO: no-cache requests can be retrieved upon validation with origin
            if (!string.IsNullOrEmpty(request.Headers[HeaderNames.CacheControl]))
            {
                if (RequestCacheControl.NoCache)
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

        internal bool ResponseIsCacheable()
        {
            // Use optional override if specified by user
            switch (CacheabilityValidator.ResponseIsCacheableOverride(HttpContext))
            {
                case OverrideResult.UseDefaultLogic:
                    break;
                case OverrideResult.DoNotCache:
                    return false;
                case OverrideResult.Cache:
                    return true;
                default:
                    throw new NotSupportedException($"Unrecognized result from {nameof(CacheabilityValidator.ResponseIsCacheableOverride)}.");
            }

            // Only cache pages explicitly marked with public
            // TODO: Consider caching responses that are not marked as public but otherwise cacheable?
            if (!ResponseCacheControl.Public)
            {
                return false;
            }

            // Check no-store
            if (RequestCacheControl.NoStore || ResponseCacheControl.NoStore)
            {
                return false;
            }

            // Check no-cache
            // TODO: Handle no-cache with headers
            if (ResponseCacheControl.NoCache)
            {
                return false;
            }

            var response = HttpContext.Response;

            // Do not cache responses varying by *
            if (string.Equals(response.Headers[HeaderNames.Vary], "*", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // TODO: public MAY override the cacheability checks for private and status codes
            
            // Check private
            if (ResponseCacheControl.Private)
            {
                return false;
            }

            // Check response code
            // TODO: RFC also lists 203, 204, 206, 300, 301, 404, 405, 410, 414, and 501 as cacheable by default
            if (response.StatusCode != StatusCodes.Status200OK)
            {
                return false;
            }

            return true;
        }

        internal bool EntryIsFresh(ResponseHeaders responseHeaders, TimeSpan age, bool verifyAgainstRequest)
        {
            var responseCacheControl = responseHeaders.CacheControl ?? EmptyCacheControl;
            
            // Add min-fresh requirements
            if (verifyAgainstRequest)
            {
                age += RequestCacheControl.MinFresh ?? TimeSpan.Zero;
            }

            // Validate shared max age, this overrides any max age settings for shared caches
            if (age > responseCacheControl.SharedMaxAge)
            {
                // shared max age implies must revalidate
                return false;
            }
            else if (responseCacheControl.SharedMaxAge == null)
            {
                // Validate max age
                if (age > responseCacheControl.MaxAge || (verifyAgainstRequest && age > RequestCacheControl.MaxAge))
                {
                    // Must revalidate
                    if (responseCacheControl.MustRevalidate)
                    {
                        return false;
                    }

                    // Request allows stale values
                    if (verifyAgainstRequest && age < RequestCacheControl.MaxStaleLimit)
                    {
                        // TODO: Add warning header indicating the response is stale
                        return true;
                    }

                    return false;
                }
                else if (responseCacheControl.MaxAge == null && (!verifyAgainstRequest || RequestCacheControl.MaxAge == null))
                {
                    // Validate expiration
                    if (_responseTime > responseHeaders.Expires)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        internal async Task<bool> TryServeFromCacheAsync()
        {
            _cacheKey = CreateCacheKey();
            var cacheEntry = Cache.Get(_cacheKey);
            var responseServed = false;

            if (cacheEntry is CachedVaryBy)
            {
                // Request contains VaryBy rules, recompute key and try again
                _cacheKey = CreateCacheKey(cacheEntry as CachedVaryBy);
                cacheEntry = Cache.Get(_cacheKey);
            }

            if (cacheEntry is CachedResponse)
            {
                var cachedResponse = cacheEntry as CachedResponse;
                var cachedResponseHeaders = new ResponseHeaders(cachedResponse.Headers);

                _responseTime = Clock.UtcNow;
                var age = _responseTime - cachedResponse.Created;
                age = age > TimeSpan.Zero ? age : TimeSpan.Zero;

                if (EntryIsFresh(cachedResponseHeaders, age, verifyAgainstRequest: true))
                {
                    var response = HttpContext.Response;
                    // Copy the cached status code and response headers
                    response.StatusCode = cachedResponse.StatusCode;
                    foreach (var header in cachedResponse.Headers)
                    {
                        response.Headers.Add(header);
                    }

                    response.Headers[HeaderNames.Age] = age.TotalSeconds.ToString("F0", CultureInfo.InvariantCulture);

                    if (_responseType == ResponseType.HeadersOnly)
                    {
                        responseServed = true;
                    }
                    else if (_responseType == ResponseType.FullReponse)
                    {
                        // Copy the cached response body
                        var body = cachedResponse.Body;

                        // Add a content-length if required
                        if (response.ContentLength == null && string.IsNullOrEmpty(response.Headers[HeaderNames.TransferEncoding]))
                        {
                            response.ContentLength = body.Length;
                        }

                        if (body.Length > 0)
                        {
                            await response.Body.WriteAsync(body, 0, body.Length);
                        }

                        responseServed = true;
                    }
                    else
                    {
                        throw new InvalidOperationException($"{nameof(_responseType)} not specified or is unrecognized.");
                    }
                }
                else
                {
                    // TODO: Validate with endpoint instead
                }
            }

            if (!responseServed && RequestCacheControl.OnlyIfCached)
            {
                HttpContext.Response.StatusCode = StatusCodes.Status504GatewayTimeout;

                responseServed = true;
            }

            return responseServed;
        }

        internal void FinalizeCachingHeaders()
        {
            if (CacheResponse)
            {
                // Create the cache entry now
                var response = HttpContext.Response;
                var varyHeaderValue = response.Headers[HeaderNames.Vary];
                _cachedResponseValidFor = ResponseCacheControl.SharedMaxAge
                    ?? ResponseCacheControl.MaxAge
                    ?? (ResponseHeaders.Expires - _responseTime)
                    // TODO: Heuristics for expiration?
                    ?? TimeSpan.FromSeconds(10);

                // Check if any VaryBy rules exist
                if (!StringValues.IsNullOrEmpty(varyHeaderValue))
                {
                    var cachedVaryBy = new CachedVaryBy
                    {
                        // Only vary by headers for now
                        // TODO: VaryBy Encoding
                        Headers = varyHeaderValue
                    };

                    // TODO: Overwrite?
                    Cache.Set(_cacheKey, cachedVaryBy, _cachedResponseValidFor);
                    _cacheKey = CreateCacheKey(cachedVaryBy);
                }

                // Ensure date header is set
                if (ResponseHeaders.Date == null)
                {
                    ResponseHeaders.Date = _responseTime;
                }

                // Store the response to cache
                _cachedResponse = new CachedResponse
                {
                    Created = ResponseHeaders.Date.Value,
                    StatusCode = HttpContext.Response.StatusCode
                };

                foreach (var header in ResponseHeaders.Headers)
                {
                    if (!string.Equals(header.Key, HeaderNames.Age, StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(header.Key, HeaderNames.SetCookie, StringComparison.OrdinalIgnoreCase))
                    {
                        _cachedResponse.Headers.Add(header);
                    }
                }
            }
            else
            {
                ResponseCacheStream.DisableBuffering();
            }
        }

        internal void FinalizeCachingBody()
        {
            if (CacheResponse && ResponseCacheStream.BufferingEnabled)
            {
                _cachedResponse.Body = ResponseCacheStream.BufferedStream.ToArray();

                Cache.Set(_cacheKey, _cachedResponse, _cachedResponseValidFor);
            }
        }

        internal void OnResponseStarting()
        {
            if (!ResponseStarted)
            {
                ResponseStarted = true;
                _responseTime = Clock.UtcNow;

                FinalizeCachingHeaders();
            }
        }

        internal void ShimResponseStream()
        {
            // TODO: Consider caching large responses on disk and serving them from there.

            // Shim response stream
            OriginalResponseStream = HttpContext.Response.Body;
            ResponseCacheStream = new ResponseCacheStream(OriginalResponseStream);
            HttpContext.Response.Body = ResponseCacheStream;

            // Shim IHttpSendFileFeature
            OriginalSendFileFeature = HttpContext.Features.Get<IHttpSendFileFeature>();
            if (OriginalSendFileFeature != null)
            {
                HttpContext.Features.Set<IHttpSendFileFeature>(new SendFileFeatureWrapper(OriginalSendFileFeature, ResponseCacheStream));
            }
        }

        internal void UnshimResponseStream()
        {
            // Unshim response stream
            HttpContext.Response.Body = OriginalResponseStream;

            // Unshim IHttpSendFileFeature
            HttpContext.Features.Set(OriginalSendFileFeature);
        }

        private enum ResponseType
        {
            HeadersOnly = 0,
            FullReponse = 1
        }
    }
}
