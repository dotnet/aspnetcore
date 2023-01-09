// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.AspNetCore.Mvc.TagHelpers.Cache;

/// <summary>
/// Implements <see cref="IDistributedCacheTagHelperStorage"/> by storing the content
/// in using <see cref="IDistributedCache"/> as the store.
/// </summary>
public class DistributedCacheTagHelperStorage : IDistributedCacheTagHelperStorage
{
    private readonly IDistributedCache _distributedCache;

    /// <summary>
    /// Creates a new <see cref="DistributedCacheTagHelperStorage"/>.
    /// </summary>
    /// <param name="distributedCache">The <see cref="IDistributedCache"/> to use.</param>
    public DistributedCacheTagHelperStorage(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    /// <inheritdoc />
    public Task<byte[]> GetAsync(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _distributedCache.GetAsync(key);
    }

    /// <inheritdoc />
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        return _distributedCache.SetAsync(key, value, options);
    }
}
