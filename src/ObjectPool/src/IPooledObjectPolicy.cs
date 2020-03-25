// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.ObjectPool
{
    /// <summary>
    /// Represents a policy for managing pooled objects.
    /// </summary>
    /// <typeparam name="T">The type of object which is being pooled.</typeparam>
    public interface IPooledObjectPolicy<T>
    {
        /// <summary>
        /// Create a <typeparamref name="T"/>.
        /// </summary>
        /// <returns>The <typeparamref name="T"/> which was created.</returns>
        T Create();

        /// <summary>
        /// Runs some processing when an object was returned to the pool. Can be used to reset the state of an object and indicate if the object should be returned to the pool.
        /// </summary>
        /// <param name="obj">The object to return to the pool.</param>
        /// <returns><see langword="true" /> if the object should be returned to the pool. <see langword="false" /> if it's not possible/desirable for the pool to keep the object.</returns>
        bool Return(T obj);
    }
}
