// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// An implementation of this interface can update how the current request is cached.
/// </summary>
public interface IOutputCachingPolicyProvider
{
    /// <summary>
    /// Returns wether the current request has any configured policy.
    /// </summary>
    /// <param name="httpContext">The current <see cref="HttpContext"/> instance.</param>
    /// <returns>True if the current request has a policy, false otherwise.</returns>
    bool HasPolicies(HttpContext httpContext);

    /// <summary>
    /// Determines whether the response caching logic should be attempted for the incoming HTTP request.
    /// </summary>
    /// <param name="context">The <see cref="IOutputCachingContext"/>.</param>
    Task OnRequestAsync(IOutputCachingContext context);

    /// <summary>
    /// Determines whether the response retrieved from the response cache is fresh and can be served.
    /// </summary>
    /// <param name="context">The <see cref="IOutputCachingContext"/>.</param>
    Task OnServeFromCacheAsync(IOutputCachingContext context);

    /// <summary>
    /// Determines whether the response can be cached for future requests.
    /// </summary>
    /// <param name="context">The <see cref="IOutputCachingContext"/>.</param>
    Task OnServeResponseAsync(IOutputCachingContext context);
}
