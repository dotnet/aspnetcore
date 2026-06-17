// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// An implementation of this interface can update how the current request is cached.
/// </summary>
public interface IOutputCachePolicy
{
    /// <summary>
    /// Updates the <see cref="OutputCacheContext"/> before the cache middleware is invoked.
    /// At that point the cache middleware can still be enabled or disabled for the request.
    /// </summary>
    /// <param name="context">The current request's cache context.</param>
    /// <param name="cancellation">The token to monitor for cancellation requests.</param>
    ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation);

    /// <summary>
    /// Updates the <see cref="OutputCacheContext"/> before the cached response is used.
    /// At that point the freshness of the cached response can be updated.
    /// </summary>
    /// <param name="context">The current request's cache context.</param>
    /// <param name="cancellation">The token to monitor for cancellation requests.</param>
    ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation);

    /// <summary>
    /// Updates the <see cref="OutputCacheContext"/> before the response is served and can be cached.
    /// At that point cacheability of the response can be updated.
    /// </summary>
    /// <param name="context">The current request's cache context.</param>
    /// <param name="cancellation">The token to monitor for cancellation requests.</param>
    ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation);
}
