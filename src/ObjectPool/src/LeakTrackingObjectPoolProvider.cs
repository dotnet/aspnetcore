// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.Extensions.ObjectPool;

/// <summary>
/// An <see cref="ObjectPoolProvider"/> that produces instances of
/// <see cref="LeakTrackingObjectPool{T}"/>.
/// </summary>
[Obsolete("LeakTrackingObjectPoolProvider was only intended for internal use in diagnostic builds of .NET. " +
    "It never functioned in publicly shipped .NET versions and may be removed in a future release.")]
public class LeakTrackingObjectPoolProvider : ObjectPoolProvider
{
    private readonly ObjectPoolProvider _inner;

    /// <summary>
    /// Initializes a new instance of <see cref="LeakTrackingObjectPoolProvider"/>.
    /// </summary>
    /// <param name="inner">The <see cref="ObjectPoolProvider"/> to wrap.</param>
    public LeakTrackingObjectPoolProvider(ObjectPoolProvider inner)
    {
        ArgumentNullThrowHelper.ThrowIfNull(inner);

        _inner = inner;
    }

    /// <inheritdoc/>
    public override ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy)
    {
        var inner = _inner.Create<T>(policy);
        return new LeakTrackingObjectPool<T>(inner);
    }
}
