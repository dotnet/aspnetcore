// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Tools
{
    /// <summary>
    /// Cache with a fixed size that evicts the least recently used members.
    /// Thread-safe.
    /// This was taken from https://github.com/dotnet/roslyn/blob/749c0ec135d7d080658dc1aa794d15229c3d10d2/src/Compilers/Core/Portable/InternalUtilities/ConcurrentLruCache.cs.
    /// </summary>
    internal class ConcurrentLruCache<TKey, TValue>
    {
        private readonly int _capacity;

        private readonly Dictionary<TKey, CacheValue> _cache;
        private readonly LinkedList<TKey> _nodeList;
        // This is a naive course-grained lock, it can probably be optimized
        private readonly object _lockObject = new object();

        public ConcurrentLruCache(int capacity)
            : this (capacity, EqualityComparer<TKey>.Default)
        {
        }

        public ConcurrentLruCache(int capacity, IEqualityComparer<TKey> comparer)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }
            _capacity = capacity;
            _cache = new Dictionary<TKey, CacheValue>(capacity, comparer);
            _nodeList = new LinkedList<TKey>();
        }

        /// <summary>
        /// Create cache from an array. The cache capacity will be the size
        /// of the array. All elements of the array will be added to the 
        /// cache. If any duplicate keys are found in the array a
        /// <see cref="ArgumentException"/> will be thrown.
        /// </summary>
        public ConcurrentLruCache(KeyValuePair<TKey, TValue>[] array)
            : this(array.Length)
        {
            foreach (var kvp in array)
            {
                UnsafeAdd(kvp.Key, kvp.Value);
            }
        }

        public int Count
        {
            get
            {
                lock (_lockObject)
                {
                    return _cache.Count;
                }
            }
        }

        public void Add(TKey key, TValue value)
        {
            lock (_lockObject)
            {
                UnsafeAdd(key, value);
            }
        }

        public TValue GetOrAdd(TKey key, TValue value)
        {
            lock (_lockObject)
            {
                if (UnsafeTryGetValue(key, out var result))
                {
                    return result;
                }
                else
                {
                    UnsafeAdd(key, value);
                    return value;
                }
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (_lockObject)
            {
                return UnsafeTryGetValue(key, out value);
            }
        }

        public bool Remove(TKey key)
        {
            lock (_lockObject)
            {
                return UnsafeRemove(key);
            }
        }

        /// <summary>
        /// For testing. Very expensive.
        /// </summary>
        internal IEnumerable<KeyValuePair<TKey, TValue>> TestingEnumerable
        {
            get
            {
                lock (_lockObject)
                {
                    foreach (var key in _nodeList)
                    {
                        var kvp = new KeyValuePair<TKey, TValue>(key, _cache[key].Value);
                        yield return kvp;
                    }
                }
            }
        }

        /// <summary>
        /// Doesn't lock.
        /// </summary>
        private bool UnsafeTryGetValue(TKey key, out TValue value)
        {
            if (_cache.TryGetValue(key, out var result))
            {
                MoveNodeToTop(result.Node);
                value = result.Value;
                return true;
            }
            else
            {
                value = default(TValue);
                return false;
            }
        }

        private void MoveNodeToTop(LinkedListNode<TKey> node)
        {
            if (!object.ReferenceEquals(_nodeList.First, node))
            {
                _nodeList.Remove(node);
                _nodeList.AddFirst(node);
            }
        }

        /// <summary>
        /// Expects non-empty cache. Does not lock.
        /// </summary>
        private void UnsafeEvictLastNode()
        {
            Debug.Assert(_capacity > 0);
            var lastNode = _nodeList.Last;
            _nodeList.Remove(lastNode);
            _cache.Remove(lastNode.Value);
        }

        private void UnsafeAddNodeToTop(TKey key, TValue value)
        {
            var node = new LinkedListNode<TKey>(key);
            _cache.Add(key, new CacheValue(value, node));
            _nodeList.AddFirst(node);
        }

        /// <summary>
        /// Doesn't lock.
        /// </summary>
        private void UnsafeAdd(TKey key, TValue value)
        {
            if (_cache.TryGetValue(key, out var result))
            {
                throw new ArgumentException("Key already exists", nameof(key));
            }
            else
            {
                if (_cache.Count == _capacity)
                {
                    UnsafeEvictLastNode();
                }
                UnsafeAddNodeToTop(key, value);
            }
        }

        private bool UnsafeRemove(TKey key)
        {
            _nodeList.Remove(key);
            return _cache.Remove(key);
        }

        private struct CacheValue
        {
            public CacheValue(TValue value, LinkedListNode<TKey> node)
            {
                Value = value;
                Node = node;
            }

            public TValue Value { get; }

            public LinkedListNode<TKey> Node { get; }
        }
    }
}
