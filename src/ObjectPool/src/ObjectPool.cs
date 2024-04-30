// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ObjectPool;

/// <summary>
/// A pool of objects.
/// </summary>
/// <typeparam name="T">The type of objects to pool.</typeparam>
public abstract class ObjectPool<T> where T : class
{
    /// <summary>
    /// Gets an object from the pool if one is available, otherwise creates one.
    /// </summary>
    /// <returns>A <typeparamref name="T"/>.</returns>
    public abstract T Get();

    /// <summary>
    /// Return an object to the pool.
    /// </summary>
    /// <param name="obj">The object to add to the pool.</param>
    public abstract void Return(T obj);
}

/// <summary>
/// Methods for creating <see cref="ObjectPool{T}"/> instances.
/// </summary>
public static class ObjectPool
{
    /// <inheritdoc cref="ObjectPoolProvider.Create{T}(IPooledObjectPolicy{T})" />
    public static ObjectPool<T> Create<T>(IPooledObjectPolicy<T>? policy = null) where T : class, new()
    {
        var provider = new DefaultObjectPoolProvider();
        return provider.Create(policy ?? new DefaultPooledObjectPolicy<T>());
    }
}
