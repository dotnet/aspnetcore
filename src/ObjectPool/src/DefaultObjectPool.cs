// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.Extensions.ObjectPool
{
    public class DefaultObjectPool<T> : ObjectPool<T> where T : class
    {
        private readonly ObjectWrapper[] _items;
        private readonly IPooledObjectPolicy<T> _policy;
        private readonly bool _isDefaultPolicy;
        private T _firstItem;

        // This class was introduced in 2.1 to avoid the interface call where possible
        private readonly PooledObjectPolicy<T> _fastPolicy;

        public DefaultObjectPool(IPooledObjectPolicy<T> policy)
            : this(policy, Environment.ProcessorCount * 2)
        {
        }

        public DefaultObjectPool(IPooledObjectPolicy<T> policy, int maximumRetained)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _fastPolicy = policy as PooledObjectPolicy<T>;
            _isDefaultPolicy = IsDefaultPolicy();

            // -1 due to _firstItem
            _items = new ObjectWrapper[maximumRetained - 1];

            bool IsDefaultPolicy()
            {
                var type = policy.GetType();

                return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(DefaultPooledObjectPolicy<>);
            }
        }

        public override T Get()
        {
            T item = _firstItem;

            if (item == null || Interlocked.CompareExchange(ref _firstItem, null, item) != item)
            {
                item = GetViaScan();
            }

            return item;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T GetViaScan()
        {
            ObjectWrapper[] items = _items;

            for (var i = 0; i < items.Length; i++)
            {
                T item = items[i];

                if (item != null && Interlocked.CompareExchange(ref items[i].Element, null, item) == item)
                {
                    return item;
                }
            }

            return Create();
        }

        // Non-inline to improve its code quality as uncommon path
        [MethodImpl(MethodImplOptions.NoInlining)]
        private T Create() => _fastPolicy?.Create() ?? _policy.Create();

        public override void Return(T obj)
        {
            if (_isDefaultPolicy || (_fastPolicy?.Return(obj) ?? _policy.Return(obj)))
            {
                if (_firstItem != null || Interlocked.CompareExchange(ref _firstItem, obj, null) != null)
                {
                    ReturnViaScan(obj);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReturnViaScan(T obj)
        {
            ObjectWrapper[] items = _items;

            for (var i = 0; i < items.Length && Interlocked.CompareExchange(ref items[i].Element, obj, null) != null; ++i)
            {
            }
        }

        [DebuggerDisplay("{Element}")]
        private struct ObjectWrapper
        {
            public T Element;

            public ObjectWrapper(T item) => Element = item;

            public static implicit operator T(ObjectWrapper wrapper) => wrapper.Element;
        }
    }
}
