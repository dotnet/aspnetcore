// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ObjectPool;

/// <summary>
/// Default implementation for <see cref="PooledObjectPolicy{T}"/>.
/// </summary>
/// <typeparam name="T">The type of object which is being pooled.</typeparam>
public class DefaultPooledObjectPolicy<T> : PooledObjectPolicy<T> where T : class, new()
{
    /// <inheritdoc />
    public override T Create()
    {
        return new T();
    }

    /// <inheritdoc />
    public override bool Return(T obj)
    {
        if (obj is IResettable resettable)
        {
            return resettable.TryReset();
        }

        return true;
    }
}
