// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Caching.Distributed;

public interface IDistributedCache<T>
{
    ValueTask<T> GetAsync(string key, Func<ValueTask<T>> callback, DistributedCacheEntryOptions? options = null, CancellationToken cancellationToken = default);

    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);
}

