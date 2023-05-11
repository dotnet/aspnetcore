// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ObjectPool;

/// <summary>
/// Contains configuration for pools.
/// </summary>
public sealed class PoolOptions
{
    /// <summary>
    /// Gets or sets the maximal capacity of the pool.
    /// </summary>
    /// <remarks>The default is 1024.</remarks>
    public int Capacity { get; set; } = 1024;
}
