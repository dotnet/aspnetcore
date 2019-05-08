// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// A collection of ordered, strongly-typed metadata associated with an <see cref="Endpoint"/>.
    /// </summary>
    /// <typeparam name="T">The metadata type.</typeparam>
    public sealed class OrderedEndpointMetadataCollection<T> : IReadOnlyList<T>
        where T : class
    {
        internal static readonly OrderedEndpointMetadataCollection<T> Empty = new OrderedEndpointMetadataCollection<T>(Array.Empty<T>());

        private readonly T[] _items;

        internal OrderedEndpointMetadataCollection(T[] items)
        {
            _items = items;
        }

        /// <summary>
        /// Gets the item at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index of the item to retrieve.</param>
        /// <returns>The item at <paramref name="index"/>.</returns>
        public T this[int index] => _items[index];

        /// <summary>
        /// Gets the count of metadata items.
        /// </summary>
        public int Count => _items.Length;

        /// <summary>
        /// Gets an <see cref="IEnumerator"/> of all metadata items.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> of all metadata items.</returns>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// Gets an <see cref="IEnumerator{T}"/> of all metadata items.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> of all metadata items.</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Gets an <see cref="IEnumerator"/> of all metadata items.
        /// </summary>
        /// <returns>An <see cref="IEnumerator"/> of all metadata items.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Enumerates the elements of an <see cref="OrderedEndpointMetadataCollection{T}"/>.
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            // Intentionally not readonly to prevent defensive struct copies
            private T[] _items;
            private int _index;

            internal Enumerator(OrderedEndpointMetadataCollection<T> collection)
            {
                _items = collection._items;
                _index = 0;
                Current = null;
            }

            /// <summary>
            /// Gets the element at the current position of the enumerator
            /// </summary>
            public T Current { get; private set; }

            /// <summary>
            /// Gets the element at the current position of the enumerator
            /// </summary>
            object IEnumerator.Current => Current;

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
