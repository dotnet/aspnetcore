// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Caching.Hybrid;

/// <summary>
/// Additional flags that apply to a <see cref="HybridCache"/> operation.
/// </summary>
[Flags]
public enum HybridCacheEntryFlags
{
    /// <summary>
    /// No additional flags.
    /// </summary>
    None = 0,
    /// <summary>
    /// Disables reading from the local in-process cache.
    /// </summary>
    DisableLocalCacheRead = 1 << 0,
    /// <summary>
    /// Disables writing to the local in-process cache.
    /// </summary>
    DisableLocalCacheWrite = 1 << 1,
    /// <summary>
    /// Disables both reading from and writing to the local in-process cache.
    /// </summary>
    DisableLocalCache = DisableLocalCacheRead | DisableLocalCacheWrite,
    /// <summary>
    /// Disables reading from the secondary distributed cache.
    /// </summary>
    DisableDistributedCacheRead = 1 << 2,
    /// <summary>
    /// Disables writing to the secondary distributed cache.
    /// </summary>
    DisableDistributedCacheWrite = 1 << 3,
    /// <summary>
    /// Disables both reading from and writing to the secondary distributed cache.
    /// </summary>
    DisableDistributedCache = DisableDistributedCacheRead | DisableDistributedCacheWrite,
    /// <summary>
    /// Only fetches the value from cache; does not attempt to access the underlying data store.
    /// </summary>
    DisableUnderlyingData = 1 << 4,
    /// <summary>
    /// Disables compression for this payload.
    /// </summary>
    DisableCompression = 1 << 5,
}
