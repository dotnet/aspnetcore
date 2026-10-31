// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ObjectPool;

/// <summary>
/// A provider of <see cref="ObjectPool{T}"/> instances.
/// </summary>
public abstract class ObjectPoolProvider
{
    /// <summary>
    /// Creates an <see cref="ObjectPool"/>.
    /// </summary>
    /// <typeparam name="T">The type to create a pool for.</typeparam>
    public ObjectPool<T> Create<T>() where T : class, new()
    {
        return Create<T>(new DefaultPooledObjectPolicy<T>());
    }

    /// <summary>
    /// Creates an <see cref="ObjectPool"/> with the given <see cref="IPooledObjectPolicy{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type to create a pool for.</typeparam>
    public abstract ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy) where T : class;
}
