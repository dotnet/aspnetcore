// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// Reads and writes the cache entry of a request in the <see cref="IOutputCacheStore"/>.
/// </summary>
internal interface IOutputCacheEntryStrategy
{
    /// <summary>
    /// Gets the cached entry matching the request, using <see cref="OutputCacheContext.CacheKey"/>.
    /// </summary>
    ValueTask<OutputCacheEntry?> GetAsync(OutputCacheContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Stores <see cref="OutputCacheContext.CachedResponse"/>, using <see cref="OutputCacheContext.CacheKey"/>.
    /// </summary>
    ValueTask StoreAsync(OutputCacheContext context, CancellationToken cancellationToken);
}
