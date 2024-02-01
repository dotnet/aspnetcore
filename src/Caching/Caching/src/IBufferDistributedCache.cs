// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.Extensions.Caching.Distributed;
public interface IBufferDistributedCache : IDistributedCache
{
    ValueTask<bool> TryGetAsync(string key, IBufferWriter<byte> destination, CancellationToken cancellationToken);
    ValueTask SetAsync(string key, ReadOnlySequence<byte> value, DistributedCacheEntryOptions options, CancellationToken cancellationToken);
}

public interface IDistributedCacheInvalidation : IDistributedCache
{
    event Func<string, ValueTask> CacheKeyInvalidated;
}
