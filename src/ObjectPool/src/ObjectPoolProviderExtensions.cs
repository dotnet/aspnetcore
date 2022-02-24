// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.Extensions.ObjectPool;

/// <summary>
/// Extension methods for <see cref="ObjectPoolProvider"/>.
/// </summary>
public static class ObjectPoolProviderExtensions
{
    /// <summary>
    /// Creates an <see cref="ObjectPool{T}"/> that pools <see cref="StringBuilder"/> instances.
    /// </summary>
    /// <param name="provider">The <see cref="ObjectPoolProvider"/>.</param>
    /// <returns>The <see cref="ObjectPool{T}"/>.</returns>
    public static ObjectPool<StringBuilder> CreateStringBuilderPool(this ObjectPoolProvider provider)
    {
        return provider.Create<StringBuilder>(new StringBuilderPooledObjectPolicy());
    }

    /// <summary>
    /// Creates an <see cref="ObjectPool{T}"/> that pools <see cref="StringBuilder"/> instances.
    /// </summary>
    /// <param name="provider">The <see cref="ObjectPoolProvider"/>.</param>
    /// <param name="initialCapacity">The initial capacity to initialize <see cref="StringBuilder"/> instances with.</param>
    /// <param name="maximumRetainedCapacity">The maximum value for <see cref="StringBuilder.Capacity"/> that is allowed to be
    /// retained, when an instance is returned.</param>
    /// <returns>The <see cref="ObjectPool{T}"/>.</returns>
    public static ObjectPool<StringBuilder> CreateStringBuilderPool(
        this ObjectPoolProvider provider,
        int initialCapacity,
        int maximumRetainedCapacity)
    {
        var policy = new StringBuilderPooledObjectPolicy()
        {
            InitialCapacity = initialCapacity,
            MaximumRetainedCapacity = maximumRetainedCapacity,
        };

        return provider.Create<StringBuilder>(policy);
    }
}
