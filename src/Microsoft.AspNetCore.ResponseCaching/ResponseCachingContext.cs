// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.ResponseCaching.Internal;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.ResponseCaching
{
    internal class ResponseCachingContext
    {
        private static readonly CacheControlHeaderValue EmptyCacheControl = new CacheControlHeaderValue();
        // Use the record separator for delimiting components of the cache key to avoid possible collisions
        private static readonly char KeyDelimiter = '\x1e';

        private readonly HttpContext _httpContext;
        private readonly IResponseCache _cache;
        private readonly ISystemClock _clock;
        private readonly ObjectPool<StringBuilder> _builderPool;
        private readonly IResponseCachingCacheabilityValidator _cacheabilityValidator;
        private readonly IResponseCachingCacheKeySuffixProvider _cacheKeySuffixProvider;

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
        
        internal ResponseCachingContext(
            HttpContext httpContext,
            IResponseCache cache,
            ObjectPool<StringBuilder> builderPool,
            IResponseCachingCacheabilityValidator cacheabilityValidator,
            IResponseCachingCacheKeySuffixProvider cacheKeySuffixProvider)
            : this(httpContext, cache, new SystemClock(), builderPool, cacheabilityValidator, cacheKeySuffixProvider)
        {
        }

        // Internal for testing
        internal ResponseCachingContext(
            HttpContext httpContext, 
            IResponseCache cache,
            ISystemClock clock,
            ObjectPool<StringBuilder> builderPool,
            IResponseCachingCacheabilityValidator cacheabilityValidator,
            IResponseCachingCacheKeySuffixProvider cacheKeySuffixProvider)
        {
            _httpContext = httpContext;
            _cache = cache;
            _clock = clock;
            _builderPool = builderPool;
            _cacheabilityValidator = cacheabilityValidator;
            _cacheKeySuffixProvider = cacheKeySuffixProvider;
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

        private Stream OriginalResponseStream { get; set; }

        private ResponseCacheStream ResponseCacheStream { get; set; }

        private IHttpSendFileFeature OriginalSendFileFeature { get; set; }

        private RequestHeaders RequestHeaders
        {
            get
            {
                if (_requestHeaders == null)
                {
                    _requestHeaders = _httpContext.Request.GetTypedHeaders();
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
                    _responseHeaders = _httpContext.Response.GetTypedHeaders();
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
            var request = _httpContext.Request;
            var builder = _builderPool.Get();

            try
            {
                // Default key 
                builder
                    .Append(request.Method.ToUpperInvariant())
                    .Append(KeyDelimiter)
                    .Append(request.Path.Value.ToUpperInvariant());

                // Vary by headers
                if (varyBy?.Headers.Count > 0)
                {
                    // Append a group separator for the header segment of the cache key
                    builder.Append(KeyDelimiter)
                        .Append('H');

                    // TODO: resolve key format and delimiters
                    foreach (var header in varyBy.Headers)
                    {
                        // TODO: Normalization of order, case?
                        var value = _httpContext.Request.Headers[header];

                        // TODO: How to handle null/empty string?
                        if (StringValues.IsNullOrEmpty(value))
                        {
                            value = "null";
                        }

                        builder.Append(KeyDelimiter)
                            .Append(header)
                            .Append("=")
                            .Append(value);
                    }
                }

                // Vary by query params
                if (varyBy?.Params.Count > 0)
                {
                    // Append a group separator for the query parameter segment of the cache key
                    builder.Append(KeyDelimiter)
                        .Append('Q');

                    if (varyBy.Params.Count == 1 && string.Equals(varyBy.Params[0], "*", StringComparison.Ordinal))
                    {
                        // Vary by all available query params
                        foreach (var query in _httpContext.Request.Query.OrderBy(q => q.Key, StringComparer.OrdinalIgnoreCase))
                        {
                            builder.Append(KeyDelimiter)
                                .Append(query.Key.ToUpperInvariant())
                                .Append("=")
                                .Append(query.Value);
                        }
                    }
                    else
                    {
                        // TODO: resolve key format and delimiters
                        foreach (var param in varyBy.Params)
                        {
                            // TODO: Normalization of order, case?
                            var value = _httpContext.Request.Query[param];

                            // TODO: How to handle null/empty string?
                            if (StringValues.IsNullOrEmpty(value))
                            {
                                value = "null";
                            }

                            builder.Append(KeyDelimiter)
                                .Append(param)
                                .Append("=")
                                .Append(value);
                        }
                    }
                }

                // Append custom cache key segment
                var customKey = _cacheKeySuffixProvider.CreateCustomKeySuffix(_httpContext);
                if (!string.IsNullOrEmpty(customKey))
                {
                    // Append a group separator for the custom segment of the cache key
                    builder.Append(KeyDelimiter)
                        .Append('C');

                    builder.Append(KeyDelimiter)
                        .Append(customKey);
                }

                return builder.ToString();
            }
            finally
            {
                _builderPool.Return(builder);
            }
        }

        internal bool RequestIsCacheable()
        {
            // Use optional override if specified by user
            switch(_cacheabilityValidator.RequestIsCacheableOverride(_httpContext))
            {
                case OverrideResult.UseDefaultLogic:
                    break;
                case OverrideResult.DoNotCache:
                    return false;
                case OverrideResult.Cache:
                    return true;
                default:
                    throw new NotSupportedException($"Unrecognized result from {nameof(_cacheabilityValidator.RequestIsCacheableOverride)}.");
            }

            // Verify the method
            // TODO: RFC lists POST as a cacheable method when explicit freshness information is provided, but this is not widely implemented. Will revisit.
            var request = _httpContext.Request;
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
            switch (_cacheabilityValidator.ResponseIsCacheableOverride(_httpContext))
            {
                case OverrideResult.UseDefaultLogic:
                    break;
                case OverrideResult.DoNotCache:
                    return false;
                case OverrideResult.Cache:
                    return true;
                default:
                    throw new NotSupportedException($"Unrecognized result from {nameof(_cacheabilityValidator.ResponseIsCacheableOverride)}.");
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

            var response = _httpContext.Response;

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
            var cacheEntry = _cache.Get(_cacheKey);
            var responseServed = false;

            if (cacheEntry is CachedVaryBy)
            {
                // Request contains VaryBy rules, recompute key and try again
                _cacheKey = CreateCacheKey(cacheEntry as CachedVaryBy);
                cacheEntry = _cache.Get(_cacheKey);
            }

            if (cacheEntry is CachedResponse)
            {
                var cachedResponse = cacheEntry as CachedResponse;
                var cachedResponseHeaders = new ResponseHeaders(cachedResponse.Headers);

                _responseTime = _clock.UtcNow;
                var age = _responseTime - cachedResponse.Created;
                age = age > TimeSpan.Zero ? age : TimeSpan.Zero;

                if (EntryIsFresh(cachedResponseHeaders, age, verifyAgainstRequest: true))
                {
                    // Check conditional request rules
                    if (ConditionalRequestSatisfied(cachedResponseHeaders))
                    {
                        _httpContext.Response.StatusCode = StatusCodes.Status304NotModified;
                        responseServed = true;
                    }
                    else
                    {
                        var response = _httpContext.Response;
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
                }
                else
                {
                    // TODO: Validate with endpoint instead
                }
            }

            if (!responseServed && RequestCacheControl.OnlyIfCached)
            {
                _httpContext.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
                responseServed = true;
            }

            return responseServed;
        }

        internal bool ConditionalRequestSatisfied(ResponseHeaders cachedResponseHeaders)
        {
            var ifNoneMatchHeader = RequestHeaders.IfNoneMatch;

            if (ifNoneMatchHeader != null)
            {
                if (ifNoneMatchHeader.Count == 1 && ifNoneMatchHeader[0].Equals(EntityTagHeaderValue.Any))
                {
                    return true;
                }

                if (cachedResponseHeaders.ETag != null)
                {
                    foreach (var tag in ifNoneMatchHeader)
                    {
                        if (cachedResponseHeaders.ETag.Compare(tag, useStrongComparison: true))
                        {
                            return true;
                        }
                    }
                }
            }
            else if ((cachedResponseHeaders.LastModified ?? cachedResponseHeaders.Date) <= RequestHeaders.IfUnmodifiedSince)
            {
                return true;
            }

            return false;
        }

        internal void FinalizeCachingHeaders()
        {
            if (CacheResponse)
            {
                // Create the cache entry now
                var response = _httpContext.Response;
                var varyHeaderValue = response.Headers[HeaderNames.Vary];
                var varyParamsValue = _httpContext.GetResponseCachingFeature().VaryByParams;
                _cachedResponseValidFor = ResponseCacheControl.SharedMaxAge
                    ?? ResponseCacheControl.MaxAge
                    ?? (ResponseHeaders.Expires - _responseTime)
                    // TODO: Heuristics for expiration?
                    ?? TimeSpan.FromSeconds(10);

                // Check if any VaryBy rules exist
                if (!StringValues.IsNullOrEmpty(varyHeaderValue) || !StringValues.IsNullOrEmpty(varyParamsValue))
                {
                    if (varyParamsValue.Count > 1)
                    {
                        Array.Sort(varyParamsValue.ToArray(), StringComparer.OrdinalIgnoreCase);
                    }

                    var cachedVaryBy = new CachedVaryBy
                    {
                        // TODO: VaryBy Encoding
                        Headers = varyHeaderValue,
                        Params = varyParamsValue
                    };

                    // TODO: Overwrite?
                    _cache.Set(_cacheKey, cachedVaryBy, _cachedResponseValidFor);
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
                    StatusCode = _httpContext.Response.StatusCode
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

                _cache.Set(_cacheKey, _cachedResponse, _cachedResponseValidFor);
            }
        }

        internal void OnResponseStarting()
        {
            if (!ResponseStarted)
            {
                ResponseStarted = true;
                _responseTime = _clock.UtcNow;

                FinalizeCachingHeaders();
            }
        }

        internal void ShimResponseStream()
        {
            // TODO: Consider caching large responses on disk and serving them from there.

            // Shim response stream
            OriginalResponseStream = _httpContext.Response.Body;
            ResponseCacheStream = new ResponseCacheStream(OriginalResponseStream);
            _httpContext.Response.Body = ResponseCacheStream;

            // Shim IHttpSendFileFeature
            OriginalSendFileFeature = _httpContext.Features.Get<IHttpSendFileFeature>();
            if (OriginalSendFileFeature != null)
            {
                _httpContext.Features.Set<IHttpSendFileFeature>(new SendFileFeatureWrapper(OriginalSendFileFeature, ResponseCacheStream));
            }

            // TODO: Move this temporary interface with endpoint to HttpAbstractions
            _httpContext.AddResponseCachingFeature();
        }

        internal void UnshimResponseStream()
        {
            // Unshim response stream
            _httpContext.Response.Body = OriginalResponseStream;

            // Unshim IHttpSendFileFeature
            _httpContext.Features.Set(OriginalSendFileFeature);

            // TODO: Move this temporary interface with endpoint to HttpAbstractions
            _httpContext.RemoveResponseCachingFeature();
        }

        private enum ResponseType
        {
            HeadersOnly = 0,
            FullReponse = 1
        }
    }
}
