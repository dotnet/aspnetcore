// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.Connections;

/// <summary>
/// Interface for creating memory pools.
/// </summary>
public interface IMemoryPoolFactory<T>
{
    /// <summary>
    /// Creates a new instance of a memory pool.
    /// </summary>
    /// <param name="options">Options for configuring the memory pool.</param>
    /// <returns>A new memory pool instance.</returns>
    MemoryPool<T> Create(MemoryPoolOptions? options = null);
}
