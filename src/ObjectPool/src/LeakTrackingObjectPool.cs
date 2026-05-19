// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.Extensions.ObjectPool;

/// <summary>
/// An <see cref="ObjectPool{T}"/> implementation that detects leaks in the use of the object pool.
/// <para>
/// A leak is produced if an object is leased from the pool but not returned before it is finalized.
/// An error is only produced in <c>Debug</c> builds.
/// This type is only recommended to be used for diagnostic builds.
/// </para>
/// </summary>
/// <typeparam name="T">The type of object which is being pooled.</typeparam>
[Obsolete("LeakTrackingObjectPool<T> was only intended for internal use in diagnostic builds of .NET. " +
    "It never functioned in publicly shipped .NET versions and may be removed in a future release.")]
public class LeakTrackingObjectPool<T> : ObjectPool<T> where T : class
{
    private readonly ConditionalWeakTable<T, Tracker> _trackers = new ConditionalWeakTable<T, Tracker>();
    private readonly ObjectPool<T> _inner;

    /// <summary>
    /// Initializes a new instance of <see cref="LeakTrackingObjectPool{T}"/>.
    /// </summary>
    /// <param name="inner">The <see cref="ObjectPool{T}"/> instance to track leaks in.</param>
    public LeakTrackingObjectPool(ObjectPool<T> inner)
    {
        ArgumentNullThrowHelper.ThrowIfNull(inner);

        _inner = inner;
    }

    /// <inheritdoc/>
    public override T Get()
    {
        var value = _inner.Get();
        _trackers.Add(value, new Tracker());
        return value;
    }

    /// <inheritdoc/>
    public override void Return(T obj)
    {
        if (_trackers.TryGetValue(obj, out var tracker))
        {
            _trackers.Remove(obj);
            tracker.Dispose();
        }

        _inner.Return(obj);
    }

    private sealed class Tracker : IDisposable
    {
        private readonly string _stack;
        private bool _disposed;

        public Tracker()
        {
            _stack = Environment.StackTrace;
        }

        public void Dispose()
        {
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        ~Tracker()
        {
            if (!_disposed && !Environment.HasShutdownStarted)
            {
                Debug.Fail($"{typeof(T).Name} was leaked. Created at: {Environment.NewLine}{_stack}");
            }
        }
    }
}
