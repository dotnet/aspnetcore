// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.NewResponseCaching.Tests;

/// <summary>
/// A policy which caches un-authenticated, GET and HEAD, 200 responses.
/// </summary>
internal class ResponseCachingOutputCachePolicy : IOutputCachePolicy
{
    public static readonly ResponseCachingOutputCachePolicy Instance = new();

    private ResponseCachingOutputCachePolicy()
    {
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        var attemptOutputCaching = AttemptOutputCaching(context);
        context.EnableOutputCaching = true;
        context.AllowCacheLookup = attemptOutputCaching;
        context.AllowCacheStorage = attemptOutputCaching;
        context.AllowLocking = true;

        // Vary by any query by default
        context.CacheVaryByRules.QueryKeys = "*";

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        if (HeaderUtilities.TryParseSeconds(context.HttpContext.Request.Headers.CacheControl, CacheControlHeaderValue.MaxAgeString, out var responseMaxAge))
        {
            if (context.CachedEntryAge <= responseMaxAge)
            {
                context.IsCacheEntryFresh = false;
            }

            context.ResponseExpirationTimeSpan = responseMaxAge;
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        var response = context.HttpContext.Response;

        // Verify existence of cookie headers
        if (!StringValues.IsNullOrEmpty(response.Headers.SetCookie))
        {
            context.AllowCacheStorage = false;
            return ValueTask.CompletedTask;
        }

        // Check response code
        if (response.StatusCode != StatusCodes.Status200OK)
        {
            context.AllowCacheStorage = false;
            return ValueTask.CompletedTask;
        }

        // Don't cache reponses that explicitly prevent caching
        if (HeaderUtilities.ContainsCacheDirective(context.HttpContext.Response.Headers.CacheControl, CacheControlHeaderValue.NoStoreString))
        {
            context.AllowCacheStorage = false;
            return ValueTask.CompletedTask;
        }

        if (HeaderUtilities.ContainsCacheDirective(context.HttpContext.Response.Headers.Pragma, CacheControlHeaderValue.NoStoreString))
        {
            context.AllowCacheStorage = false;
            return ValueTask.CompletedTask;
        }

        // Check request no-store
        if (HeaderUtilities.ContainsCacheDirective(context.HttpContext.Request.Headers.CacheControl, CacheControlHeaderValue.NoStoreString))
        {
            context.AllowCacheStorage = false;
            return ValueTask.CompletedTask;
        }

        return ValueTask.CompletedTask;
    }

    private static bool AttemptOutputCaching(OutputCacheContext context)
    {
        context.CacheVaryByRules.HeaderNames = context.CacheVaryByRules.HeaderNames.Concat(context.HttpContext.Response.Headers.Vary).ToArray();

        // Check if the current request fulfills the requirements to be cached

        var request = context.HttpContext.Request;

        // Verify the method
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
        {
            return false;
        }

        // Verify existence of authorization headers
        if (!StringValues.IsNullOrEmpty(request.Headers.Authorization) || request.HttpContext.User?.Identity?.IsAuthenticated == true)
        {
            return false;
        }

        // From Response Caching Middleware

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
}

