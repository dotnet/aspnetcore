// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.Extensions.ObjectPool
{
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

        private bool ReturnCore(T obj)
        {
            bool returnedTooPool = false;

            if (_isDefaultPolicy || (_fastPolicy?.Return(obj) ?? _policy.Return(obj)))
            {
                if (_firstItem == null && Interlocked.CompareExchange(ref _firstItem, obj, null) == null)
                {
                    returnedTooPool = true;
                }
                else
                {
                    var items = _items;
                    for (var i = 0; i < items.Length && !(returnedTooPool = Interlocked.CompareExchange(ref items[i].Element, obj, null) == null); i++)
                    {
                    }
                }
            }

            return returnedTooPool;
        }

        public void Dispose()
        {
            _isDisposed = true;

            DisposeItem(_firstItem);
            _firstItem = null;

            ObjectWrapper[] items = _items;
            for (var i = 0; i < items.Length; i++)
            {
                DisposeItem(items[i].Element);
                items[i].Element = null;
            }
        }

        private void DisposeItem(T item)
        {
            if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
