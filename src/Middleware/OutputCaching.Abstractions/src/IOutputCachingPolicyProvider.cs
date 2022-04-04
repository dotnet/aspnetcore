// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// An implementation of this interface can update how the current request is cached.
/// </summary>
public interface IOutputCachingPolicyProvider
{
    /// <summary>
    /// Determine whether the response caching logic should be attempted for the incoming HTTP request.
    /// </summary>
    /// <param name="context">The <see cref="OutputCachingContext"/>.</param>
    /// <returns><c>true</c> if response caching logic should be attempted; otherwise <c>false</c>.</returns>
    Task OnRequestAsync(IOutputCachingContext context);

    /// <summary>
    /// Determine whether the response retrieved from the response cache is fresh and can be served.
    /// </summary>
    /// <param name="context">The <see cref="OutputCachingContext"/>.</param>
    /// <returns><c>true</c> if cache lookup for this cache entry is allowed; otherwise <c>false</c>.</returns>
    Task OnServeFromCacheAsync(IOutputCachingContext context);

    /// <summary>
    /// Determine whether the response can be cached for future requests.
    /// </summary>
    /// <param name="context">The <see cref="OutputCachingContext"/>.</param>
    /// <returns><c>true</c> if cache lookup for this request is allowed; otherwise <c>false</c>.</returns>
    Task OnServeResponseAsync(IOutputCachingContext context);
}
