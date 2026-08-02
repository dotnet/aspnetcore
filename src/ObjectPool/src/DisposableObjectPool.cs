// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Microsoft.Extensions.ObjectPool;

internal sealed class DisposableObjectPool<T> : DefaultObjectPool<T>, IDisposable where T : class
{
    private volatile bool _isDisposed;

    public DisposableObjectPool(IPooledObjectPolicy<T> policy)
        : base(policy)
    {
    }

    public DisposableObjectPool(IPooledObjectPolicy<T> policy, int maximumRetained)
        : base(policy, maximumRetained)
    {
    }

    public override T Get()
    {
        if (_isDisposed)
        {
            ThrowObjectDisposedException();
        }

        return base.Get();

        void ThrowObjectDisposedException()
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    public override void Return(T obj)
    {
        // When the pool is disposed or the obj is not returned to the pool, dispose it
        if (_isDisposed || !ReturnCore(obj))
        {
            DisposeItem(obj);
        }
    }

    public void Dispose()
    {
        _isDisposed = true;

        DisposeItem(_fastItem);
        _fastItem = null;

        while (_items.TryDequeue(out var item))
        {
            DisposeItem(item);
        }
    }

    private static void DisposeItem(T? item)
    {
        if (item is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
