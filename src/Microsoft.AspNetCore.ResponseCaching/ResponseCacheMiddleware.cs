// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.ResponseCaching.Internal;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.ResponseCaching
{
    public class ResponseCacheMiddleware
    {
        private static readonly TimeSpan DefaultExpirationTimeSpan = TimeSpan.FromSeconds(10);

        private readonly RequestDelegate _next;
        private readonly IResponseCacheStore _store;
        private readonly ResponseCacheOptions _options;
        private readonly IResponseCachePolicyProvider _policyProvider;
        private readonly IResponseCacheKeyProvider _keyProvider;
        private readonly Func<object, Task> _onStartingCallback;

        public ResponseCacheMiddleware(
            RequestDelegate next,
            IResponseCacheStore store,
            IOptions<ResponseCacheOptions> options,
            IResponseCachePolicyProvider policyProvider,
            IResponseCacheKeyProvider keyProvider)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (policyProvider == null)
            {
                throw new ArgumentNullException(nameof(policyProvider));
            }
            if (keyProvider == null)
            {
                throw new ArgumentNullException(nameof(keyProvider));
            }

            _next = next;
            _store = store;
            _options = options.Value;
            _policyProvider = policyProvider;
            _keyProvider = keyProvider;
            _onStartingCallback = state => OnResponseStartingAsync((ResponseCacheContext)state);
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var context = new ResponseCacheContext(httpContext);

            // Should we attempt any caching logic?
            if (_policyProvider.IsRequestCacheable(context))
            {
                // Can this request be served from cache?
                if (await TryServeFromCacheAsync(context))
                {
                    return;
                }

                // Hook up to listen to the response stream
                ShimResponseStream(context);

                try
                {
                    // Subscribe to OnStarting event
                    httpContext.Response.OnStarting(_onStartingCallback, context);

                    await _next(httpContext);

                    // If there was no response body, check the response headers now. We can cache things like redirects.
                    await OnResponseStartingAsync(context);

                    // Finalize the cache entry
                    await FinalizeCacheBodyAsync(context);
                }
                finally
                {
                    UnshimResponseStream(context);
                }
            }
            else
            {
                await _next(httpContext);
            }
        }

        internal async Task<bool> TryServeCachedResponseAsync(ResponseCacheContext context, CachedResponse cachedResponse)
        {
            context.CachedResponse = cachedResponse;
            context.CachedResponseHeaders = new ResponseHeaders(cachedResponse.Headers);
            context.ResponseTime = _options.SystemClock.UtcNow;
            var cachedEntryAge = context.ResponseTime.Value - context.CachedResponse.Created;
            context.CachedEntryAge = cachedEntryAge > TimeSpan.Zero ? cachedEntryAge : TimeSpan.Zero;

            if (_policyProvider.IsCachedEntryFresh(context))
            {
                // Check conditional request rules
                if (ConditionalRequestSatisfied(context))
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status304NotModified;
                }
                else
                {
                    var response = context.HttpContext.Response;
                    // Copy the cached status code and response headers
                    response.StatusCode = context.CachedResponse.StatusCode;
                    foreach (var header in context.CachedResponse.Headers)
                    {
                        response.Headers.Add(header);
                    }

                    response.Headers[HeaderNames.Age] = context.CachedEntryAge.Value.TotalSeconds.ToString("F0", CultureInfo.InvariantCulture);

                    // Copy the cached response body
                    var body = context.CachedResponse.Body;
                    if (body.Length > 0)
                    {
                        // Add a content-length if required
                        if (!response.ContentLength.HasValue && StringValues.IsNullOrEmpty(response.Headers[HeaderNames.TransferEncoding]))
                        {
                            response.ContentLength = body.Length;
                        }

                        try
                        {
                            await body.CopyToAsync(response.Body, StreamUtilities.BodySegmentSize, context.HttpContext.RequestAborted);
                        }
                        catch (OperationCanceledException)
                        {
                            context.HttpContext.Abort();
                        }
                    }
                }

                return true;
            }

            return false;
        }

        internal async Task<bool> TryServeFromCacheAsync(ResponseCacheContext context)
        {
            context.BaseKey = _keyProvider.CreateBaseKey(context);
            var cacheEntry = await _store.GetAsync(context.BaseKey);

            if (cacheEntry is CachedVaryByRules)
            {
                // Request contains vary rules, recompute key(s) and try again
                context.CachedVaryByRules = (CachedVaryByRules)cacheEntry;

                foreach (var varyKey in _keyProvider.CreateLookupVaryByKeys(context))
                {
                    cacheEntry = await _store.GetAsync(varyKey);

                    if (cacheEntry is CachedResponse && await TryServeCachedResponseAsync(context, (CachedResponse)cacheEntry))
                    {
                        return true;
                    }
                }
            }
            else if (cacheEntry is CachedResponse && await TryServeCachedResponseAsync(context, (CachedResponse)cacheEntry))
            {
                return true;
            }


            if (context.RequestCacheControlHeaderValue.OnlyIfCached)
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
                return true;
            }

            return false;
        }

        internal async Task FinalizeCacheHeadersAsync(ResponseCacheContext context)
        {
            if (_policyProvider.IsResponseCacheable(context))
            {
                context.ShouldCacheResponse = true;

                // Create the cache entry now
                var response = context.HttpContext.Response;
                var varyHeaders = new StringValues(response.Headers.GetCommaSeparatedValues(HeaderNames.Vary));
                var varyQueryKeys = context.HttpContext.GetResponseCacheFeature()?.VaryByQueryKeys ?? StringValues.Empty;
                context.CachedResponseValidFor = context.ResponseCacheControlHeaderValue.SharedMaxAge ??
                    context.ResponseCacheControlHeaderValue.MaxAge ??
                    (context.ResponseExpires - context.ResponseTime.Value) ??
                    DefaultExpirationTimeSpan;

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
                    await _store.SetAsync(context.BaseKey, context.CachedVaryByRules, context.CachedResponseValidFor);

                    context.StorageVaryKey = _keyProvider.CreateStorageVaryByKey(context);
                }

                // Ensure date header is set
                if (!context.ResponseDate.HasValue)
                {
                    context.ResponseDate = context.ResponseTime.Value;
                    // Setting the date on the raw response headers.
                    context.TypedResponseHeaders.Date = context.ResponseDate;
                }

                // Store the response on the state
                context.CachedResponse = new CachedResponse
                {
                    Created = context.ResponseDate.Value,
                    StatusCode = context.HttpContext.Response.StatusCode
                };

                foreach (var header in context.TypedResponseHeaders.Headers)
                {
                    if (!string.Equals(header.Key, HeaderNames.Age, StringComparison.OrdinalIgnoreCase))
                    {
                        context.CachedResponse.Headers.Add(header);
                    }
                }
            }
            else
            {
                context.ResponseCacheStream.DisableBuffering();
            }
        }

        internal async Task FinalizeCacheBodyAsync(ResponseCacheContext context)
        {
            var contentLength = context.TypedResponseHeaders.ContentLength;
            if (context.ShouldCacheResponse && context.ResponseCacheStream.BufferingEnabled)
            {
                var bufferStream = context.ResponseCacheStream.GetBufferStream();
                if (!contentLength.HasValue || contentLength == bufferStream.Length)
                {
                    context.CachedResponse.Body = bufferStream;
                    await _store.SetAsync(context.StorageVaryKey ?? context.BaseKey, context.CachedResponse, context.CachedResponseValidFor);
                }
            }
        }

        internal Task OnResponseStartingAsync(ResponseCacheContext context)
        {
            if (!context.ResponseStarted)
            {
                context.ResponseStarted = true;
                context.ResponseTime = _options.SystemClock.UtcNow;

                return FinalizeCacheHeadersAsync(context);
            }
            else
            {
                return TaskCache.CompletedTask;
            }
        }

        internal void ShimResponseStream(ResponseCacheContext context)
        {
            // Shim response stream
            context.OriginalResponseStream = context.HttpContext.Response.Body;
            context.ResponseCacheStream = new ResponseCacheStream(context.OriginalResponseStream, _options.MaximumBodySize, StreamUtilities.BodySegmentSize);
            context.HttpContext.Response.Body = context.ResponseCacheStream;

            // Shim IHttpSendFileFeature
            context.OriginalSendFileFeature = context.HttpContext.Features.Get<IHttpSendFileFeature>();
            if (context.OriginalSendFileFeature != null)
            {
                context.HttpContext.Features.Set<IHttpSendFileFeature>(new SendFileFeatureWrapper(context.OriginalSendFileFeature, context.ResponseCacheStream));
            }

            context.HttpContext.AddResponseCacheFeature();
        }

        internal static void UnshimResponseStream(ResponseCacheContext context)
        {
            // Unshim response stream
            context.HttpContext.Response.Body = context.OriginalResponseStream;

            // Unshim IHttpSendFileFeature
            context.HttpContext.Features.Set(context.OriginalSendFileFeature);

            context.HttpContext.RemoveResponseCacheFeature();
        }

        internal static bool ConditionalRequestSatisfied(ResponseCacheContext context)
        {
            var cachedResponseHeaders = context.CachedResponseHeaders;
            var ifNoneMatchHeader = context.TypedRequestHeaders.IfNoneMatch;

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
            else
            {
                var ifUnmodifiedSince = context.TypedRequestHeaders.IfUnmodifiedSince;
                if (ifUnmodifiedSince != null && (cachedResponseHeaders.LastModified ?? cachedResponseHeaders.Date) <= ifUnmodifiedSince)
                {
                    return true;
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
