// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// A collection of arbitrary metadata associated with an endpoint.
    /// </summary>
    /// <remarks>
    /// <see cref="EndpointMetadataCollection"/> instances contain a list of metadata items
    /// of arbitrary types. The metadata items are stored as an ordered collection with
    /// items arranged in ascending order of precedence.
    /// </remarks>
    public sealed class EndpointMetadataCollection : IReadOnlyList<object>
    {
        /// <summary>
        /// An empty <see cref="EndpointMetadataCollection"/>.
        /// </summary>
        public static readonly EndpointMetadataCollection Empty = new EndpointMetadataCollection(Array.Empty<object>());

        private readonly object[] _items;
        private readonly ConcurrentDictionary<Type, object[]> _cache;

        /// <summary>
        /// Creates a new <see cref="EndpointMetadataCollection"/>.
        /// </summary>
        /// <param name="items">The metadata items.</param>
        public EndpointMetadataCollection(IEnumerable<object> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            _items = items.ToArray();
            _cache = new ConcurrentDictionary<Type, object[]>();
        }

        /// <summary>
        /// Creates a new <see cref="EndpointMetadataCollection"/>.
        /// </summary>
        /// <param name="items">The metadata items.</param>
        public EndpointMetadataCollection(params object[] items)
            : this((IEnumerable<object>)items)
        {
        }

        /// <summary>
        /// Gets the item at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index of the item to retrieve.</param>
        /// <returns>The item at <paramref name="index"/>.</returns>
        public object this[int index] => _items[index];

        /// <summary>
        /// Gets the count of metadata items.
        /// </summary>
        public int Count => _items.Length;

        /// <summary>
        /// Gets the most significant metadata item of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of metadata to retrieve.</typeparam>
        /// <returns>
        /// The most significant metadata of type <typeparamref name="T"/> or <c>null</c>.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? GetMetadata<T>() where T : class
        {
            if (_cache.TryGetValue(typeof(T), out var obj))
            {
                var result = (T[])obj;
                var length = result.Length;
                return length > 0 ? result[length - 1] : default;
            }

            return GetMetadataSlow<T>();
        }

        private T? GetMetadataSlow<T>() where T : class
        {
            var result = GetOrderedMetadataSlow<T>();
            var length = result.Length;
            return length > 0 ? result[length - 1] : default;
        }

        /// <summary>
        /// Gets the metadata items of type <typeparamref name="T"/> in ascending
        /// order of precedence.
        /// </summary>
        /// <typeparam name="T">The type of metadata.</typeparam>
        /// <returns>A sequence of metadata items of <typeparamref name="T"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IReadOnlyList<T> GetOrderedMetadata<T>() where T : class
        {
            if (_cache.TryGetValue(typeof(T), out var result))
            {
                return (T[])result;
            }

            return GetOrderedMetadataSlow<T>();
        }

        private T[] GetOrderedMetadataSlow<T>() where T : class
        {
            // Perf: avoid allocations totally for the common case where there are no matching metadata.
            List<T>? matches = null;

            var items = _items;
            for (var i = 0; i < items.Length; i++)
            {
                if (items[i] is T item)
                {
                    matches ??= new List<T>();
                    matches.Add(item);
                }
            }

            var results = matches == null ? Array.Empty<T>() : matches.ToArray();
            _cache.TryAdd(typeof(T), results);
            return results;
        }

        /// <summary>
        /// Gets an <see cref="IEnumerator"/> of all metadata items.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> of all metadata items.</returns>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// Gets an <see cref="IEnumerator{Object}"/> of all metadata items.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{Object}"/> of all metadata items.</returns>
        IEnumerator<object> IEnumerable<object>.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Gets an <see cref="IEnumerator"/> of all metadata items.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> of all metadata items.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Enumerates the elements of an <see cref="EndpointMetadataCollection"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<object?>
        {
            // Intentionally not readonly to prevent defensive struct copies
            private readonly object[] _items;
            private int _index;

            internal Enumerator(EndpointMetadataCollection collection)
            {
                _items = collection._items;
                _index = 0;
                Current = null;
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator
            /// </summary>
            public object? Current { get; private set; }

            /// <summary>
            /// Releases all resources used by the <see cref="Enumerator"/>.
            /// </summary>
            public void Dispose()
            {
            }

            /// <summary>
            /// Advances the enumerator to the next element of the <see cref="Enumerator"/>.
            /// </summary>
            /// <returns>
            /// <c>true</c> if the enumerator was successfully advanced to the next element;
            /// <c>false</c> if the enumerator has passed the end of the collection.
            /// </returns>
            public bool MoveNext()
            {
                if (_index < _items.Length)
                {
                    Current = _items[_index++];
                    return true;
                }

                Current = null;
                return false;
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            public void Reset()
            {
                _index = 0;
                Current = null;
            }
        }
    }
}
