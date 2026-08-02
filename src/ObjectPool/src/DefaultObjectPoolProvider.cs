// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.Extensions.ObjectPool;

/// <summary>
/// The default <see cref="ObjectPoolProvider"/>.
/// </summary>
public class DefaultObjectPoolProvider : ObjectPoolProvider
{
    /// <summary>
    /// The maximum number of objects to retain in the pool.
    /// </summary>
    public int MaximumRetained { get; set; } = Environment.ProcessorCount * 2;

    /// <inheritdoc/>
    public override ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy)
    {
        ArgumentNullThrowHelper.ThrowIfNull(policy);

        if (typeof(IDisposable).IsAssignableFrom(typeof(T)))
        {
            return new DisposableObjectPool<T>(policy, MaximumRetained);
        }

        return new DefaultObjectPool<T>(policy, MaximumRetained);
    }
}
