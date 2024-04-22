// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.Extensions.Caching.Hybrid;

/// <summary>
/// Additional options (expiration, etc.) that apply to a <see cref="HybridCache"/> operation. When options
/// can be specified at multiple levels (for example, globally and per-call), the values are composed; the
/// most granular non-null value is used, with null values being inherited. If no value is specified at
/// any level, the implementation may choose a reasonable default.
/// </summary>
public sealed class HybridCacheEntryOptions
{
    /// <summary>
    /// Overall cache duration of this entry, passed to the backend distributed cache.
    /// </summary>
    public TimeSpan? Expiration { get; init; } // overall cache duration

    /// <summary>
    /// Cache duration in local cache; when retrieving a cached value
    /// from an external cache store, this value will be used to calculate the local
    /// cache expiration, not exceeding the remaining overall cache lifetime.
    /// </summary>
    public TimeSpan? LocalCacheExpiration { get; init; } // TTL in L1

    /// <summary>
    /// Additional flags that apply to this usage.
    /// </summary>
    public HybridCacheEntryFlags? Flags { get; init; }

    // memoize when possible
    private DistributedCacheEntryOptions? _dc;
    internal DistributedCacheEntryOptions? ToDistributedCacheEntryOptions()
        => Expiration is null ? null : (_dc ??= new() { AbsoluteExpirationRelativeToNow = Expiration });
}
