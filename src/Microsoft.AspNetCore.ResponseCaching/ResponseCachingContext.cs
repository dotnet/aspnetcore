// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.ResponseCaching.Internal;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.ResponseCaching
{
    internal class ResponseCachingContext
    {
        private readonly HttpContext _httpContext;
        private readonly IResponseCache _cache;
        private readonly ResponseCachingOptions _options;
        private readonly ICacheabilityValidator _cacheabilityValidator;
        private readonly IKeyProvider _keyProvider;

        private ResponseCachingState _state;

        internal ResponseCachingContext(
            HttpContext httpContext,
            IResponseCache cache,
            ResponseCachingOptions options,
            ICacheabilityValidator cacheabilityValidator,
            IKeyProvider keyProvider)
        {
            _httpContext = httpContext;
            _cache = cache;
            _options = options;
            _cacheabilityValidator = cacheabilityValidator;
            _keyProvider = keyProvider;
        }

        internal ResponseCachingState State
        {
            get
            {
                if (_state == null)
                {
                    _state = _httpContext.GetResponseCachingState();
                }
                return _state;
            }
        }

        internal bool ResponseStarted { get; set; }

        private Stream OriginalResponseStream { get; set; }

        private ResponseCacheStream ResponseCacheStream { get; set; }

        private IHttpSendFileFeature OriginalSendFileFeature { get; set; }

        internal async Task<bool> TryServeFromCacheAsync()
        {
            State.BaseKey = _keyProvider.CreateBaseKey(_httpContext);
            var cacheEntry = _cache.Get(State.BaseKey);
            var responseServed = false;

            if (cacheEntry is CachedVaryRules)
            {
                // Request contains vary rules, recompute key and try again
                State.CachedVaryRules = cacheEntry as CachedVaryRules;
                var varyKey = _keyProvider.CreateVaryKey(_httpContext, ((CachedVaryRules)cacheEntry).VaryRules);
                cacheEntry = _cache.Get(varyKey);
            }

            if (cacheEntry is CachedResponse)
            {
                State.CachedResponse = cacheEntry as CachedResponse;
                var cachedResponseHeaders = new ResponseHeaders(State.CachedResponse.Headers);

                State.ResponseTime = _options.SystemClock.UtcNow;
                var cachedEntryAge = State.ResponseTime - State.CachedResponse.Created;
                State.CachedEntryAge = cachedEntryAge > TimeSpan.Zero ? cachedEntryAge : TimeSpan.Zero;

                if (_cacheabilityValidator.CachedEntryIsFresh(_httpContext, cachedResponseHeaders))
                {
                    responseServed = true;

                    // Check conditional request rules
                    if (ConditionalRequestSatisfied(cachedResponseHeaders))
                    {
                        _httpContext.Response.StatusCode = StatusCodes.Status304NotModified;
                    }
                    else
                    {
                        var response = _httpContext.Response;
                        // Copy the cached status code and response headers
                        response.StatusCode = State.CachedResponse.StatusCode;
                        foreach (var header in State.CachedResponse.Headers)
                        {
                            response.Headers.Add(header);
                        }

                        response.Headers[HeaderNames.Age] = State.CachedEntryAge.TotalSeconds.ToString("F0", CultureInfo.InvariantCulture);

                        var body = State.CachedResponse.Body ??
                            ((CachedResponseBody)_cache.Get(State.CachedResponse.BodyKeyPrefix))?.Body;

                        // If the body is not found, something went wrong.
                        if (body == null)
                        {
                            return false;
                        }

                        // Copy the cached response body
                        if (body.Length > 0)
                        {
                            // Add a content-length if required
                            if (response.ContentLength == null && StringValues.IsNullOrEmpty(response.Headers[HeaderNames.TransferEncoding]))
                            {
                                response.ContentLength = body.Length;
                            }
                            await response.Body.WriteAsync(body, 0, body.Length);
                        }
                    }
                }
                else
                {
                    // TODO: Validate with endpoint instead
                }
            }

            if (!responseServed && State.RequestCacheControl.OnlyIfCached)
            {
                _httpContext.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
                responseServed = true;
            }

            return responseServed;
        }

        internal bool ConditionalRequestSatisfied(ResponseHeaders cachedResponseHeaders)
        {
            var ifNoneMatchHeader = State.RequestHeaders.IfNoneMatch;

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
            else if ((cachedResponseHeaders.LastModified ?? cachedResponseHeaders.Date) <= State.RequestHeaders.IfUnmodifiedSince)
            {
                return true;
            }

            return false;
        }

        internal void FinalizeCachingHeaders()
        {
            if (_cacheabilityValidator.ResponseIsCacheable(_httpContext))
            {
                State.ShouldCacheResponse = true;

                // Create the cache entry now
                var response = _httpContext.Response;
                var varyHeaderValue = response.Headers[HeaderNames.Vary];
                var varyParamsValue = _httpContext.GetResponseCachingFeature()?.VaryParams ?? StringValues.Empty;
                State.CachedResponseValidFor = State.ResponseCacheControl.SharedMaxAge
                    ?? State.ResponseCacheControl.MaxAge
                    ?? (State.ResponseHeaders.Expires - State.ResponseTime)
                    // TODO: Heuristics for expiration?
                    ?? TimeSpan.FromSeconds(10);

                // Check if any vary rules exist
                if (!StringValues.IsNullOrEmpty(varyHeaderValue) || !StringValues.IsNullOrEmpty(varyParamsValue))
                {
                    // Normalize order and casing of vary by rules
                    var normalizedVaryHeaderValue = GetNormalizedStringValues(varyHeaderValue);
                    var normalizedVaryParamsValue = GetNormalizedStringValues(varyParamsValue);

                    // Update vary rules if they are different
                    if (State.CachedVaryRules == null ||
                        !StringValues.Equals(State.CachedVaryRules.VaryRules.Params, normalizedVaryParamsValue) ||
                        !StringValues.Equals(State.CachedVaryRules.VaryRules.Headers, normalizedVaryHeaderValue))
                    {
                        var cachedVaryRules = new CachedVaryRules
                        {
                            VaryKeyPrefix = FastGuid.NewGuid().IdString,
                            VaryRules = new VaryRules()
                            {
                                // TODO: Vary Encoding
                                Headers = normalizedVaryHeaderValue,
                                Params = normalizedVaryParamsValue
                            }
                        };

                        State.CachedVaryRules = cachedVaryRules;
                        _cache.Set(State.BaseKey, cachedVaryRules, State.CachedResponseValidFor);
                    }

                    State.VaryKey = _keyProvider.CreateVaryKey(_httpContext, State.CachedVaryRules.VaryRules);
                }

                // Ensure date header is set
                if (State.ResponseHeaders.Date == null)
                {
                    State.ResponseHeaders.Date = State.ResponseTime;
                }

                // Store the response on the state
                State.CachedResponse = new CachedResponse
                {
                    BodyKeyPrefix = FastGuid.NewGuid().IdString,
                    Created = State.ResponseHeaders.Date.Value,
                    StatusCode = _httpContext.Response.StatusCode
                };

                foreach (var header in State.ResponseHeaders.Headers)
                {
                    if (!string.Equals(header.Key, HeaderNames.Age, StringComparison.OrdinalIgnoreCase))
                    {
                        State.CachedResponse.Headers.Add(header);
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
            if (State.ShouldCacheResponse && ResponseCacheStream.BufferingEnabled)
            {
                if (ResponseCacheStream.BufferedStream.Length >= _options.MinimumSplitBodySize)
                {
                    // Store response and response body separately
                    _cache.Set(State.VaryKey ?? State.BaseKey, State.CachedResponse, State.CachedResponseValidFor);

                    var cachedResponseBody = new CachedResponseBody()
                    {
                        Body = ResponseCacheStream.BufferedStream.ToArray()
                    };

                    _cache.Set(State.CachedResponse.BodyKeyPrefix, cachedResponseBody, State.CachedResponseValidFor);
                }
                else
                {
                    // Store response and response body together
                    State.CachedResponse.Body = ResponseCacheStream.BufferedStream.ToArray();
                    _cache.Set(State.VaryKey ?? State.BaseKey, State.CachedResponse, State.CachedResponseValidFor);
                }
            }
        }

        internal void OnResponseStarting()
        {
            if (!ResponseStarted)
            {
                ResponseStarted = true;
                State.ResponseTime = _options.SystemClock.UtcNow;

                FinalizeCachingHeaders();
            }
        }

        internal void ShimResponseStream()
        {
            // TODO: Consider caching large responses on disk and serving them from there.

            // Shim response stream
            OriginalResponseStream = _httpContext.Response.Body;
            ResponseCacheStream = new ResponseCacheStream(OriginalResponseStream, _options.MaximumCachedBodySize);
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

        // Normalize order and casing
        internal static StringValues GetNormalizedStringValues(StringValues stringVales)
        {
            if (stringVales.Count == 1)
            {
                return new StringValues(stringVales.ToString().ToUpperInvariant());
            }
            else
            {
                var originalArray = stringVales.ToArray();
                var newArray = new string[originalArray.Length];

                for (int i = 0; i < originalArray.Length; i++)
                {
                    newArray[i] = originalArray[i].ToUpperInvariant();
                }

                // Since the casing has already been normalized, use Ordinal comparison
                Array.Sort(newArray, StringComparer.Ordinal);

                return new StringValues(newArray);
            }
        }
    }
}
