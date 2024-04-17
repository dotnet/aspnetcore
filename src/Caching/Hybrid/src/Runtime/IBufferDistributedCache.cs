// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Caching.Distributed; // intentional for parity with IDistributedCache

/// <summary>
/// Represents a distributed cache of serialized values, with support for low allocation data transfer.
/// </summary>
public interface IBufferDistributedCache : IDistributedCache
{
    /// <summary>
    /// Attempt to retrieve an existing cache item.
    /// </summary>
    /// <param name="key">The unique key for the cache item.</param>
    /// <param name="destination">The target to write the cache contents on success.</param>
    /// <returns><c>true</c> if the cache item is found, <c>false</c> otherwise.</returns>
    /// <remarks>This is functionally similar to <see cref="IDistributedCache.Get(string)"/>, but avoids the array allocation.</remarks>
    bool TryGet(string key, IBufferWriter<byte> destination);

    /// <summary>
    /// Asynchronously attempt to retrieve an existing cache entry.
    /// </summary>
    /// <param name="key">The unique key for the cache entry.</param>
    /// <param name="destination">The target to write the cache contents on success.</param>
    /// <param name="token">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <returns><c>true</c> if the cache entry is found, <c>false</c> otherwise.</returns>
    /// <remarks>This is functionally similar to <see cref="IDistributedCache.GetAsync(string, CancellationToken)"/>, but avoids the array allocation.</remarks>
    ValueTask<bool> TryGetAsync(string key, IBufferWriter<byte> destination, CancellationToken token = default);

    /// <summary>
    /// Sets or overwrites a cache item.
    /// </summary>
    /// <param name="key">The key of the entry to create.</param>
    /// <param name="value">The value for this cache entry.</param>
    /// <param name="options">The cache options for the entry.</param>
    /// <remarks>This is functionally similar to <see cref="IDistributedCache.Set(string, byte[], DistributedCacheEntryOptions)"/>, but avoids the array allocation.</remarks>
    void Set(string key, ReadOnlySequence<byte> value, DistributedCacheEntryOptions options);

    /// <summary>
    /// Asynchronously sets or overwrites a cache entry.
    /// </summary>
    /// <param name="key">The key of the entry to create.</param>
    /// <param name="value">The value for this cache entry.</param>
    /// <param name="options">The cache options for the value.</param>
    /// <param name="token">The <see cref="CancellationToken"/> used to propagate notifications that the operation should be canceled.</param>
    /// <remarks>This is functionally similar to <see cref="IDistributedCache.SetAsync(string, byte[], DistributedCacheEntryOptions, CancellationToken)"/>, but avoids the array allocation.</remarks>
    ValueTask SetAsync(string key, ReadOnlySequence<byte> value, DistributedCacheEntryOptions options, CancellationToken token = default);
}
