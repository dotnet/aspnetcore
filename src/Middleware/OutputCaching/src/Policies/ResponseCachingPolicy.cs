// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// A policy which applies the HTTP caching semantics of the Response Caching middleware:
/// request and response <c>Cache-Control</c> directives decide whether a response is cached, for how long, and whether a cached entry can be served.
/// </summary>
/// <remarks>
/// Unlike Response Caching, the cache key can't be extended by the response <c>Vary</c> header.
/// Responses varying by headers which are not declared upfront (e.g. with <see cref="OutputCachePolicyBuilder.SetVaryByHeader(string[])"/>) are not stored.
/// </remarks>
internal sealed class ResponseCachingPolicy : IOutputCachePolicy
{
    internal static readonly TimeSpan DefaultExpirationTimeSpan = TimeSpan.FromSeconds(10);

    public static readonly ResponseCachingPolicy Instance = new();

    private ResponseCachingPolicy()
    {
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        var attemptResponseCaching = AttemptResponseCaching(context);
        context.EnableOutputCaching = true;
        context.AllowCacheLookup = attemptResponseCaching && AllowCacheLookup(context);
        context.AllowCacheStorage = attemptResponseCaching && AllowCacheStorage(context);

        // A request which bypasses the cache lookup must not be served a response produced by a concurrent request.
        context.AllowLocking = context.AllowCacheLookup;

        // The cache key is computed before the response is produced, so query keys can't be selected by the response.
        context.CacheVaryByRules.QueryKeys = "*";

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        if (!IsCachedEntryFresh(context))
        {
            context.IsCacheEntryFresh = false;
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        if (!context.AllowCacheStorage)
        {
            return ValueTask.CompletedTask;
        }

        var responseTime = context.ResponseTime ?? DateTimeOffset.UtcNow;

        if (!IsResponseCacheable(context, responseTime, out var responseSharedMaxAge, out var responseMaxAge, out var responseExpires)
            || HasUndeclaredVaryHeaders(context))
        {
            context.AllowCacheStorage = false;
            return ValueTask.CompletedTask;
        }

        context.ResponseExpirationTimeSpan = responseSharedMaxAge
            ?? responseMaxAge
            ?? (responseExpires - responseTime)
            ?? context.ResponseExpirationTimeSpan
            ?? DefaultExpirationTimeSpan;

        return ValueTask.CompletedTask;
    }

    internal static bool AttemptResponseCaching(OutputCacheContext context)
    {
        var request = context.HttpContext.Request;

        // Verify the method
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
        {
            return false;
        }

        // Verify existence of authorization headers
        if (!StringValues.IsNullOrEmpty(request.Headers.Authorization))
        {
            return false;
        }

        return true;
    }

    internal static bool AllowCacheLookup(OutputCacheContext context)
    {
        var requestHeaders = context.HttpContext.Request.Headers;
        var cacheControl = requestHeaders.CacheControl;

        // Verify request cache-control parameters
        if (!StringValues.IsNullOrEmpty(cacheControl))
        {
            if (HeaderUtilities.ContainsCacheDirective(cacheControl, CacheControlHeaderValue.NoCacheString))
            {
                return false;
            }
        }
        else
        {
            // Support for legacy HTTP 1.0 cache directive
            if (HeaderUtilities.ContainsCacheDirective(requestHeaders.Pragma, CacheControlHeaderValue.NoCacheString))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool AllowCacheStorage(OutputCacheContext context)
    {
        // Check request no-store
        return !HeaderUtilities.ContainsCacheDirective(context.HttpContext.Request.Headers.CacheControl, CacheControlHeaderValue.NoStoreString);
    }

    internal static bool IsResponseCacheable(OutputCacheContext context, DateTimeOffset responseTime, out TimeSpan? responseSharedMaxAge, out TimeSpan? responseMaxAge, out DateTimeOffset? responseExpires)
    {
        var response = context.HttpContext.Response;
        var responseCacheControlHeader = response.Headers.CacheControl;

        HeaderUtilities.TryParseSeconds(responseCacheControlHeader, CacheControlHeaderValue.SharedMaxAgeString, out responseSharedMaxAge);
        HeaderUtilities.TryParseSeconds(responseCacheControlHeader, CacheControlHeaderValue.MaxAgeString, out responseMaxAge);
        responseExpires = HeaderUtilities.TryParseDate(response.Headers.Expires.ToString(), out var expires) ? expires : null;

        // Only cache pages explicitly marked with public
        if (!HeaderUtilities.ContainsCacheDirective(responseCacheControlHeader, CacheControlHeaderValue.PublicString))
        {
            return false;
        }

        // Check response no-store
        if (HeaderUtilities.ContainsCacheDirective(responseCacheControlHeader, CacheControlHeaderValue.NoStoreString))
        {
            return false;
        }

        // Check no-cache
        if (HeaderUtilities.ContainsCacheDirective(responseCacheControlHeader, CacheControlHeaderValue.NoCacheString))
        {
            return false;
        }

        // Do not cache responses with Set-Cookie headers
        if (!StringValues.IsNullOrEmpty(response.Headers.SetCookie))
        {
            return false;
        }

        // Do not cache responses varying by *
        foreach (var varyHeader in GetVaryHeaders(response))
        {
            if (varyHeader == "*")
            {
                return false;
            }
        }

        // Check private
        if (HeaderUtilities.ContainsCacheDirective(responseCacheControlHeader, CacheControlHeaderValue.PrivateString))
        {
            return false;
        }

        // Check response code
        if (response.StatusCode != StatusCodes.Status200OK)
        {
            return false;
        }

        // Check response freshness
        DateTimeOffset? responseDate = HeaderUtilities.TryParseDate(response.Headers.Date.ToString(), out var date) ? date : null;
        if (!responseDate.HasValue)
        {
            if (!responseSharedMaxAge.HasValue &&
                !responseMaxAge.HasValue &&
                responseTime >= responseExpires)
            {
                return false;
            }
        }
        else
        {
            var age = responseTime - responseDate.Value;

            // Validate shared max age
            if (age >= responseSharedMaxAge)
            {
                return false;
            }
            else if (!responseSharedMaxAge.HasValue)
            {
                // Validate max age
                if (age >= responseMaxAge)
                {
                    return false;
                }
                else if (!responseMaxAge.HasValue)
                {
                    // Validate expiration
                    if (responseTime >= responseExpires)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    internal static bool IsCachedEntryFresh(OutputCacheContext context)
    {
        var age = context.CachedEntryAge;
        var cachedCacheControlHeaders = context.CachedResponse?.FindHeader(HeaderNames.CacheControl) ?? default;
        var requestCacheControlHeaders = context.HttpContext.Request.Headers.CacheControl;

        // Add min-fresh requirements
        if (HeaderUtilities.TryParseSeconds(requestCacheControlHeaders, CacheControlHeaderValue.MinFreshString, out var minFresh))
        {
            age += minFresh.Value;
        }

        // Validate shared max age, this overrides any max age settings for shared caches
        HeaderUtilities.TryParseSeconds(cachedCacheControlHeaders, CacheControlHeaderValue.SharedMaxAgeString, out var cachedSharedMaxAge);

        if (age >= cachedSharedMaxAge)
        {
            // shared max age implies must revalidate
            return false;
        }
        else if (!cachedSharedMaxAge.HasValue)
        {
            HeaderUtilities.TryParseSeconds(requestCacheControlHeaders, CacheControlHeaderValue.MaxAgeString, out var requestMaxAge);
            HeaderUtilities.TryParseSeconds(cachedCacheControlHeaders, CacheControlHeaderValue.MaxAgeString, out var cachedMaxAge);

            var lowestMaxAge = cachedMaxAge < requestMaxAge ? cachedMaxAge : requestMaxAge ?? cachedMaxAge;
            // Validate max age
            if (age >= lowestMaxAge)
            {
                // Must revalidate or proxy revalidate
                if (HeaderUtilities.ContainsCacheDirective(cachedCacheControlHeaders, CacheControlHeaderValue.MustRevalidateString)
                    || HeaderUtilities.ContainsCacheDirective(cachedCacheControlHeaders, CacheControlHeaderValue.ProxyRevalidateString))
                {
                    return false;
                }

                var maxStaleExist = HeaderUtilities.ContainsCacheDirective(requestCacheControlHeaders, CacheControlHeaderValue.MaxStaleString);
                HeaderUtilities.TryParseSeconds(requestCacheControlHeaders, CacheControlHeaderValue.MaxStaleString, out var requestMaxStale);

                // Request allows stale values with no age limit
                if (maxStaleExist && !requestMaxStale.HasValue)
                {
                    return true;
                }

                // Request allows stale values with age limit
                if (requestMaxStale.HasValue && age - lowestMaxAge < requestMaxStale)
                {
                    return true;
                }

                return false;
            }
            else if (!cachedMaxAge.HasValue && !requestMaxAge.HasValue)
            {
                // Validate expiration
                var cachedExpires = context.CachedResponse?.FindHeader(HeaderNames.Expires) ?? default;
                if (HeaderUtilities.TryParseDate(cachedExpires.ToString(), out var expires) &&
                    context.ResponseTime!.Value >= expires)
                {
                    return false;
                }
            }
        }

        return true;
    }

    // Response Caching adds the request values of the Vary headers to the cache key after the response is produced.
    // Output Caching computes the cache key before, so responses varying by headers which are not part of it can't be stored.
    internal static bool HasUndeclaredVaryHeaders(OutputCacheContext context)
    {
        var declaredHeaderNames = context.CacheVaryByRules.HeaderNames;

        foreach (var varyHeader in GetVaryHeaders(context.HttpContext.Response))
        {
            var isDeclared = false;
            foreach (var declaredHeaderName in declaredHeaderNames)
            {
                if (string.Equals(varyHeader, declaredHeaderName, StringComparison.OrdinalIgnoreCase))
                {
                    isDeclared = true;
                    break;
                }
            }

            if (!isDeclared)
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<string> GetVaryHeaders(HttpResponse response)
    {
        foreach (var value in response.Headers.Vary)
        {
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            foreach (var segment in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                yield return segment;
            }
        }
    }
}
