// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.ObjectPool
{
    /// <summary>
    /// A pool of objects.
    /// </summary>
    /// <typeparam name="T">The type of objects to pool.</typeparam>
    public abstract class ObjectPool<T> where T : class
    {
        /// <summary>
        /// Returns an object from the pool.
        /// </summary>
        /// <returns>An object from the pool.</returns>
        public abstract T Get();

        /// <summary>
        /// Put an object in the pool.
        /// </summary>
        /// <param name="obj">The object to add to the pool.</param>
        public abstract void Return(T obj);
    }

    /// <summary>
    /// Methods for creating <see cref="ObjectPool{T}"/>'s.
    /// </summary>
    public static class ObjectPool
    {
        /// <inheritdoc />
        public static ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy = null) where T : class, new()
        {
            var provider = new DefaultObjectPoolProvider();
            return provider.Create(policy ?? new DefaultPooledObjectPolicy<T>());
        }
    }
}
