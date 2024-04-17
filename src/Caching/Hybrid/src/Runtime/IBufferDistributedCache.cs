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
    /// <param name="destination">Target to write the cache contents on success.</param>
    /// <returns><c>True</c> if the cache item is found, <c>False</c> otherwise.</returns>
    /// <remarks>This is functionally similar to <see cref="IDistributedCache.Get(string)"/>, but avoiding the array allocation.</remarks>
    bool TryGet(string key, IBufferWriter<byte> destination);

    /// <summary>
    /// Attempt to asynchronously retrieve an existing cache item.
    /// </summary>
    /// <param name="key">The unique key for the cache item.</param>
    /// <param name="destination">Target to write the cache contents on success.</param>
    /// <param name="token">Cancellation for this operation.</param>
    /// <returns><c>True</c> if the cache item is found, <c>False</c> otherwise.</returns>
    /// <remarks>This is functionally similar to <see cref="IDistributedCache.GetAsync(string, CancellationToken)"/>, but avoiding the array allocation.</remarks>
    ValueTask<bool> TryGetAsync(string key, IBufferWriter<byte> destination, CancellationToken token = default);

    /// <summary>
    /// Insert or overwrite a cache item.
    /// </summary>
    /// <param name="key">The unique key for the cache item.</param>
    /// <param name="value">The value for this cache item.</param>
    /// <param name="options">The cache options for the value.</param>
    /// <remarks>This is functionally similar to <see cref="IDistributedCache.Set(string, byte[], DistributedCacheEntryOptions)"/>, but avoiding the array allocation.</remarks>
    void Set(string key, ReadOnlySequence<byte> value, DistributedCacheEntryOptions options);
    /// <summary>
    /// Asynchronously insert or overwrite a cache item.
    /// </summary>
    /// <param name="key">The unique key for the cache item.</param>
    /// <param name="value">The value for this cache item.</param>
    /// <param name="options">The cache options for the value.</param>
    /// <param name="token">Cancellation for this operation.</param>
    /// <remarks>This is functionally similar to <see cref="IDistributedCache.SetAsync(string, byte[], DistributedCacheEntryOptions, CancellationToken)"/>, but avoiding the array allocation.</remarks>
    ValueTask SetAsync(string key, ReadOnlySequence<byte> value, DistributedCacheEntryOptions options, CancellationToken token = default);
}
