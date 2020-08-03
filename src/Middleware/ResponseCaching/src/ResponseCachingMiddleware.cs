// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.ResponseCaching
{
    /// <summary>
    /// Enable HTTP response caching.
    /// </summary>
    public class ResponseCachingMiddleware
    {
        private static readonly TimeSpan DefaultExpirationTimeSpan = TimeSpan.FromSeconds(10);

        // see https://tools.ietf.org/html/rfc7232#section-4.1
        private static readonly string[] HeadersToIncludeIn304 =
            new[] { "Cache-Control", "Content-Location", "Date", "ETag", "Expires", "Vary" };

        private readonly RequestDelegate _next;
        private readonly ResponseCachingOptions _options;
        private readonly ILogger _logger;
        private readonly IResponseCachingPolicyProvider _policyProvider;
        private readonly IResponseCache _cache;
        private readonly IResponseCachingKeyProvider _keyProvider;

        /// <summary>
        /// Creates a new <see cref="ResponseCachingMiddleware"/>.
        /// </summary>
        /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
        /// <param name="options">The options for this middleware.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> used for logging.</param>
        /// <param name="poolProvider">The <see cref="ObjectPoolProvider"/> used for creating <see cref="ObjectPool"/> instances.</param>
        public ResponseCachingMiddleware(
            RequestDelegate next,
            IOptions<ResponseCachingOptions> options,
            ILoggerFactory loggerFactory,
            ObjectPoolProvider poolProvider)
            : this(
                next,
                options,
                loggerFactory,
                new ResponseCachingPolicyProvider(),
                new MemoryResponseCache(new MemoryCache(new MemoryCacheOptions
                {
                    SizeLimit = options.Value.SizeLimit
                })),
                new ResponseCachingKeyProvider(poolProvider, options))
        { }

        // for testing
        internal ResponseCachingMiddleware(
            RequestDelegate next,
            IOptions<ResponseCachingOptions> options,
            ILoggerFactory loggerFactory,
            IResponseCachingPolicyProvider policyProvider,
            IResponseCache cache,
            IResponseCachingKeyProvider keyProvider)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }
            if (policyProvider == null)
            {
                throw new ArgumentNullException(nameof(policyProvider));
            }
            if (cache == null)
            {
                throw new ArgumentNullException(nameof(cache));
            }
            if (keyProvider == null)
            {
                throw new ArgumentNullException(nameof(keyProvider));
            }

            _next = next;
            _options = options.Value;
            _logger = loggerFactory.CreateLogger<ResponseCachingMiddleware>();
            _policyProvider = policyProvider;
            _cache = cache;
            _keyProvider = keyProvider;
        }

        /// <summary>
        /// Invokes the logic of the middleware.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
        /// <returns>A <see cref="Task"/> that completes when the middleware has completed processing.</returns>
        public async Task Invoke(HttpContext httpContext)
        {
            var context = new ResponseCachingContext(httpContext, _logger);

            // Should we attempt any caching logic?
            if (_policyProvider.AttemptResponseCaching(context))
            {
                // Can this request be served from cache?
                if (_policyProvider.AllowCacheLookup(context) && await TryServeFromCacheAsync(context))
                {
                    return;
                }

                // Should we store the response to this request?
                if (_policyProvider.AllowCacheStorage(context))
                {
                    // Hook up to listen to the response stream
                    ShimResponseStream(context);

                    try
                    {
                        await _next(httpContext);

                        // If there was no response body, check the response headers now. We can cache things like redirects.
                        StartResponse(context);

                        // Finalize the cache entry
                        FinalizeCacheBody(context);
                    }
                    finally
                    {
                        UnshimResponseStream(context);
                    }

                    return;
                }
            }

            // Response should not be captured but add IResponseCachingFeature which may be required when the response is generated
            AddResponseCachingFeature(httpContext);

            try
            {
                await _next(httpContext);
            }
            finally
            {
                RemoveResponseCachingFeature(httpContext);
            }
        }

        internal async Task<bool> TryServeCachedResponseAsync(ResponseCachingContext context, IResponseCacheEntry cacheEntry)
        {
            if (!(cacheEntry is CachedResponse cachedResponse))
            {
                return false;
            }

            context.CachedResponse = cachedResponse;
            context.CachedResponseHeaders = cachedResponse.Headers;
            context.ResponseTime = _options.SystemClock.UtcNow;
            var cachedEntryAge = context.ResponseTime.Value - context.CachedResponse.Created;
            context.CachedEntryAge = cachedEntryAge > TimeSpan.Zero ? cachedEntryAge : TimeSpan.Zero;

            if (_policyProvider.IsCachedEntryFresh(context))
            {
                // Check conditional request rules
                if (ContentIsNotModified(context))
                {
                    _logger.NotModifiedServed();
                    context.HttpContext.Response.StatusCode = StatusCodes.Status304NotModified;

                    if (context.CachedResponseHeaders != null)
                    {
                        foreach (var key in HeadersToIncludeIn304)
                        {
                            if (context.CachedResponseHeaders.TryGetValue(key, out var values))
                            {
                                context.HttpContext.Response.Headers[key] = values;
                            }
                        }
                    }
                }
                else
                {
                    var response = context.HttpContext.Response;
                    // Copy the cached status code and response headers
                    response.StatusCode = context.CachedResponse.StatusCode;
                    foreach (var header in context.CachedResponse.Headers)
                    {
                        response.Headers[header.Key] = header.Value;
                    }

                    // Note: int64 division truncates result and errors may be up to 1 second. This reduction in
                    // accuracy of age calculation is considered appropriate since it is small compared to clock
                    // skews and the "Age" header is an estimate of the real age of cached content.
                    response.Headers[HeaderNames.Age] = HeaderUtilities.FormatNonNegativeInt64(context.CachedEntryAge.Value.Ticks / TimeSpan.TicksPerSecond);

                    // Copy the cached response body
                    var body = context.CachedResponse.Body;
                    if (body.Length > 0)
                    {
                        try
                        {
                            await body.CopyToAsync(response.BodyWriter, context.HttpContext.RequestAborted);
                        }
                        catch (OperationCanceledException)
                        {
                            context.HttpContext.Abort();
                        }
                    }
                    _logger.CachedResponseServed();
                }
                return true;
            }

            return false;
        }

        internal async Task<bool> TryServeFromCacheAsync(ResponseCachingContext context)
        {
            context.BaseKey = _keyProvider.CreateBaseKey(context);
            var cacheEntry = _cache.Get(context.BaseKey);

            if (cacheEntry is CachedVaryByRules cachedVaryByRules)
            {
                // Request contains vary rules, recompute key(s) and try again
                context.CachedVaryByRules = cachedVaryByRules;

                foreach (var varyKey in _keyProvider.CreateLookupVaryByKeys(context))
                {
                    if (await TryServeCachedResponseAsync(context, _cache.Get(varyKey)))
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (await TryServeCachedResponseAsync(context, cacheEntry))
                {
                    return true;
                }
            }

            if (HeaderUtilities.ContainsCacheDirective(context.HttpContext.Request.Headers[HeaderNames.CacheControl], CacheControlHeaderValue.OnlyIfCachedString))
            {
                _logger.GatewayTimeoutServed();
                context.HttpContext.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
                return true;
            }

            _logger.NoResponseServed();
            return false;
        }


        /// <summary>
        /// Finalize cache headers.
        /// </summary>
        /// <param name="context"></param>
        /// <returns><c>true</c> if a vary by entry needs to be stored in the cache; otherwise <c>false</c>.</returns>
        private bool OnFinalizeCacheHeaders(ResponseCachingContext context)
        {
            if (_policyProvider.IsResponseCacheable(context))
            {
                var storeVaryByEntry = false;
                context.ShouldCacheResponse = true;

                // Create the cache entry now
                var response = context.HttpContext.Response;
                var varyHeaders = new StringValues(response.Headers.GetCommaSeparatedValues(HeaderNames.Vary));
                var varyQueryKeys = new StringValues(context.HttpContext.Features.Get<IResponseCachingFeature>()?.VaryByQueryKeys);
                context.CachedResponseValidFor = context.ResponseSharedMaxAge ??
                    context.ResponseMaxAge ??
                    (context.ResponseExpires - context.ResponseTime.Value) ??
                    DefaultExpirationTimeSpan;

                // Generate a base key if none exist
                if (string.IsNullOrEmpty(context.BaseKey))
                {
                    context.BaseKey = _keyProvider.CreateBaseKey(context);
                }

                // Check if any vary rules exist
                if (!StringValues.IsNullOrEmpty(varyHeaders) || !StringValues.IsNullOrEmpty(varyQueryKeys))
                {
                    // Normalize order and casing of vary by rules
                    var normalizedVaryHeaders = GetOrderCasingNormalizedStringValues(varyHeaders);
                    var normalizedVaryQueryKeys = GetOrderCasingNormalizedStringValues(varyQueryKeys);

                    // Update vary rules if they are different
                    if (context.CachedVaryByRules == null ||
                        !StringValues.Equals(context.CachedVaryByRules.QueryKeys, normalizedVaryQueryKeys) ||
                        !StringValues.Equals(context.CachedVaryByRules.Headers, normalizedVaryHeaders))
                    {
                        context.CachedVaryByRules = new CachedVaryByRules
                        {
                            VaryByKeyPrefix = FastGuid.NewGuid().IdString,
                            Headers = normalizedVaryHeaders,
                            QueryKeys = normalizedVaryQueryKeys
                        };
                    }

                    // Always overwrite the CachedVaryByRules to update the expiry information
                    _logger.VaryByRulesUpdated(normalizedVaryHeaders, normalizedVaryQueryKeys);
                    storeVaryByEntry = true;

                    context.StorageVaryKey = _keyProvider.CreateStorageVaryByKey(context);
                }

                // Ensure date header is set
                if (!context.ResponseDate.HasValue)
                {
                    context.ResponseDate = context.ResponseTime.Value;
                    // Setting the date on the raw response headers.
                    context.HttpContext.Response.Headers[HeaderNames.Date] = HeaderUtilities.FormatDate(context.ResponseDate.Value);
                }

                // Store the response on the state
                context.CachedResponse = new CachedResponse
                {
                    Created = context.ResponseDate.Value,
                    StatusCode = context.HttpContext.Response.StatusCode,
                    Headers = new HeaderDictionary()
                };

                foreach (var header in context.HttpContext.Response.Headers)
                {
                    if (!string.Equals(header.Key, HeaderNames.Age, StringComparison.OrdinalIgnoreCase))
                    {
                        context.CachedResponse.Headers[header.Key] = header.Value;
                    }
                }

                return storeVaryByEntry;
            }

            context.ResponseCachingStream.DisableBuffering();
            return false;
        }

        internal void FinalizeCacheHeaders(ResponseCachingContext context)
        {
            if (OnFinalizeCacheHeaders(context))
            {
                _cache.Set(context.BaseKey, context.CachedVaryByRules, context.CachedResponseValidFor);
            }
        }

        internal void FinalizeCacheBody(ResponseCachingContext context)
        {
            if (context.ShouldCacheResponse && context.ResponseCachingStream.BufferingEnabled)
            {
                var contentLength = context.HttpContext.Response.ContentLength;
                var cachedResponseBody = context.ResponseCachingStream.GetCachedResponseBody();
                if (!contentLength.HasValue || contentLength == cachedResponseBody.Length
                    || (cachedResponseBody.Length == 0
                        && HttpMethods.IsHead(context.HttpContext.Request.Method)))
                {
                    var response = context.HttpContext.Response;
                    // Add a content-length if required
                    if (!response.ContentLength.HasValue && StringValues.IsNullOrEmpty(response.Headers[HeaderNames.TransferEncoding]))
                    {
                        context.CachedResponse.Headers[HeaderNames.ContentLength] = HeaderUtilities.FormatNonNegativeInt64(cachedResponseBody.Length);
                    }

                    context.CachedResponse.Body = cachedResponseBody;
                    _logger.ResponseCached();
                    _cache.Set(context.StorageVaryKey ?? context.BaseKey, context.CachedResponse, context.CachedResponseValidFor);
                }
                else
                {
                    _logger.ResponseContentLengthMismatchNotCached();
                }
            }
            else
            {
                _logger.LogResponseNotCached();
            }
        }

        /// <summary>
        /// Mark the response as started and set the response time if no reponse was started yet.
        /// </summary>
        /// <param name="context"></param>
        /// <returns><c>true</c> if the response was not started before this call; otherwise <c>false</c>.</returns>
        private bool OnStartResponse(ResponseCachingContext context)
        {
            if (!context.ResponseStarted)
            {
                context.ResponseStarted = true;
                context.ResponseTime = _options.SystemClock.UtcNow;

                return true;
            }
            return false;
        }

        internal void StartResponse(ResponseCachingContext context)
        {
            if (OnStartResponse(context))
            {
                FinalizeCacheHeaders(context);
            }
        }

        internal static void AddResponseCachingFeature(HttpContext context)
        {
            if (context.Features.Get<IResponseCachingFeature>() != null)
            {
                throw new InvalidOperationException($"Another instance of {nameof(ResponseCachingFeature)} already exists. Only one instance of {nameof(ResponseCachingMiddleware)} can be configured for an application.");
            }
            context.Features.Set<IResponseCachingFeature>(new ResponseCachingFeature());
        }

        internal void ShimResponseStream(ResponseCachingContext context)
        {
            // Shim response stream
            context.OriginalResponseStream = context.HttpContext.Response.Body;
            context.ResponseCachingStream = new ResponseCachingStream(
                context.OriginalResponseStream,
                _options.MaximumBodySize,
                StreamUtilities.BodySegmentSize,
                () => StartResponse(context));
            context.HttpContext.Response.Body = context.ResponseCachingStream;

            // Add IResponseCachingFeature
            AddResponseCachingFeature(context.HttpContext);
        }

        internal static void RemoveResponseCachingFeature(HttpContext context) =>
            context.Features.Set<IResponseCachingFeature>(null);

        internal static void UnshimResponseStream(ResponseCachingContext context)
        {
            // Unshim response stream
            context.HttpContext.Response.Body = context.OriginalResponseStream;

            // Remove IResponseCachingFeature
            RemoveResponseCachingFeature(context.HttpContext);
        }

        internal static bool ContentIsNotModified(ResponseCachingContext context)
        {
            var cachedResponseHeaders = context.CachedResponseHeaders;
            var ifNoneMatchHeader = context.HttpContext.Request.Headers[HeaderNames.IfNoneMatch];

            if (!StringValues.IsNullOrEmpty(ifNoneMatchHeader))
            {
                if (ifNoneMatchHeader.Count == 1 && StringSegment.Equals(ifNoneMatchHeader[0], EntityTagHeaderValue.Any.Tag, StringComparison.OrdinalIgnoreCase))
                {
                    context.Logger.NotModifiedIfNoneMatchStar();
                    return true;
                }

                EntityTagHeaderValue eTag;
                IList<EntityTagHeaderValue> ifNoneMatchEtags;
                if (!StringValues.IsNullOrEmpty(cachedResponseHeaders[HeaderNames.ETag])
                    && EntityTagHeaderValue.TryParse(cachedResponseHeaders[HeaderNames.ETag].ToString(), out eTag)
                    && EntityTagHeaderValue.TryParseList(ifNoneMatchHeader, out ifNoneMatchEtags))
                {
                    for (var i = 0; i < ifNoneMatchEtags.Count; i++)
                    {
                        var requestETag = ifNoneMatchEtags[i];
                        if (eTag.Compare(requestETag, useStrongComparison: false))
                        {
                            context.Logger.NotModifiedIfNoneMatchMatched(requestETag);
                            return true;
                        }
                    }
                }
            }
            else
            {
                var ifModifiedSince = context.HttpContext.Request.Headers[HeaderNames.IfModifiedSince];
                if (!StringValues.IsNullOrEmpty(ifModifiedSince))
                {
                    DateTimeOffset modified;
                    if (!HeaderUtilities.TryParseDate(cachedResponseHeaders[HeaderNames.LastModified].ToString(), out modified) &&
                        !HeaderUtilities.TryParseDate(cachedResponseHeaders[HeaderNames.Date].ToString(), out modified))
                    {
                        return false;
                    }

                    DateTimeOffset modifiedSince;
                    if (HeaderUtilities.TryParseDate(ifModifiedSince.ToString(), out modifiedSince) &&
                        modified <= modifiedSince)
                    {
                        context.Logger.NotModifiedIfModifiedSinceSatisfied(modified, modifiedSince);
                        return true;
                    }
                }
            }

            return false;
        }

        // Normalize order and casing
        internal static StringValues GetOrderCasingNormalizedStringValues(StringValues stringValues)
        {
            if (stringValues.Count == 1)
            {
                return new StringValues(stringValues.ToString().ToUpperInvariant());
            }
            else
            {
                var originalArray = stringValues.ToArray();
                var newArray = new string[originalArray.Length];

                for (var i = 0; i < originalArray.Length; i++)
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
