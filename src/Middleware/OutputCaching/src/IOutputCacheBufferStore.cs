// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// Represents a store for cached responses that uses a <see cref="IBufferWriter{Byte}"/> as the target.
/// </summary>
public interface IOutputCacheBufferStore : IOutputCacheStore
{
    /// <summary>
    /// Gets the cached response for the given key, if it exists.
    /// If no cached response exists for the given key, <c>null</c> is returned.
    /// </summary>
    /// <param name="key">The cache key to look up.</param>
    /// <param name="destination">The location to which the value should be written.</param>
    /// <param name="cancellationToken">Indicates that the operation should be cancelled.</param>
    /// <returns><c>True</c> if the response cache entry if it exists; otherwise <c>False</c>.</returns>
    ValueTask<bool> TryGetAsync(string key, PipeWriter destination, CancellationToken cancellationToken);

    /// <summary>
    /// Stores the given response in the response cache.
    /// </summary>
    /// <param name="key">The cache key to store the response under.</param>
    /// <param name="value">The response cache entry to store; this value is only defined for the duration of the method, and should not be stored without making a copy.</param>
    /// <param name="tags">The tags associated with the cache entry to store.</param>
    /// <param name="validFor">The amount of time the entry will be kept in the cache before expiring, relative to now.</param>
    /// <param name="cancellationToken">Indicates that the operation should be cancelled.</param>
    ValueTask SetAsync(string key, ReadOnlySequence<byte> value, ReadOnlyMemory<string> tags, TimeSpan validFor, CancellationToken cancellationToken);
}
