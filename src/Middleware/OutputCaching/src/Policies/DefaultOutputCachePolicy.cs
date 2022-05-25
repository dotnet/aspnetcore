// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// The default policy.
/// </summary>
internal sealed class DefaultOutputCachePolicy : IOutputCachingPolicy
{
    /// <inheritdoc />
    Task IOutputCachingPolicy.OnRequestAsync(IOutputCachingContext context)
    {
        context.AttemptOutputCaching = AttemptOutputCaching(context);
        context.AllowCacheLookup = true;
        context.AllowCacheStorage = true;
        context.AllowLocking = true;
        context.IsResponseCacheable = true;

        // Vary by any query by default
        context.CachedVaryByRules.QueryKeys = "*";

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    Task IOutputCachingPolicy.OnServeFromCacheAsync(IOutputCachingContext context)
    {
        context.IsCacheEntryFresh = true;

        // Validate expiration
        if (context.CachedEntryAge <= TimeSpan.Zero)
        {
            context.Logger.ExpirationExpiresExceeded(context.ResponseTime!.Value);
            context.IsCacheEntryFresh = false;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    Task IOutputCachingPolicy.OnServeResponseAsync(IOutputCachingContext context)
    {
        var response = context.HttpContext.Response;

        // Verify existence of cookie headers
        if (!StringValues.IsNullOrEmpty(response.Headers.SetCookie))
        {
            context.Logger.ResponseWithSetCookieNotCacheable();
            context.IsResponseCacheable = false;
            return Task.CompletedTask;
        }

        // Check response code
        if (response.StatusCode != StatusCodes.Status200OK)
        {
            context.Logger.ResponseWithUnsuccessfulStatusCodeNotCacheable(response.StatusCode);
            context.IsResponseCacheable = false;
            return Task.CompletedTask;
        }

        context.IsResponseCacheable = true;
        return Task.CompletedTask;
    }

    private static bool AttemptOutputCaching(IOutputCachingContext context)
    {
        // TODO: Should it come from options such that it can be changed without a custom default policy?

        var request = context.HttpContext.Request;

        // Verify the method
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
        {
            context.Logger.RequestMethodNotCacheable(request.Method);
            return false;
        }

        // Verify existence of authorization headers
        if (!StringValues.IsNullOrEmpty(request.Headers.Authorization) || request.HttpContext.User?.Identity?.IsAuthenticated == true)
        {
            context.Logger.RequestWithAuthorizationNotCacheable();
            return false;
        }

        return true;
    }
}
