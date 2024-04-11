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
    /// Do not read from the local in-process cache.
    /// </summary>
    DisableLocalCacheRead = 1 << 0,
    /// <summary>
    /// Do not write to the local in-process cache.
    /// </summary>
    DisableLocalCacheWrite = 1 << 1,
    /// <summary>
    /// Do not use the local in-process cache for reads or writes.
    /// </summary>
    DisableLocalCache = DisableLocalCacheRead | DisableLocalCacheWrite,
    /// <summary>
    /// Do not read from the secondary distributed cache.
    /// </summary>
    DisableDistributedCacheRead = 1 << 2,
    /// <summary>
    /// Do not write to the secondary distributed cache.
    /// </summary>
    DisableDistributedCacheWrite = 1 << 3,
    /// <summary>
    /// Do not use the local in-process cache for reads or writes.
    /// </summary>
    DisableDistributedCache = DisableDistributedCacheRead | DisableDistributedCacheWrite,
    /// <summary>
    /// Only fetch the value from cache - do not attempt to access the underlying data store.
    /// </summary>
    DisableUnderlyingData = 1 << 4,
    /// <summary>
    /// Do not compress this payload.
    /// </summary>
    DisableCompression = 1 << 5,
}
