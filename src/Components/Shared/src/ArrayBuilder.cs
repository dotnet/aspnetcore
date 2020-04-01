// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#if IGNITOR
namespace Ignitor
#elif COMPONENTS_SERVER
namespace Microsoft.AspNetCore.Components.Server.Circuits
#else
namespace Microsoft.AspNetCore.Components.RenderTree
#endif
{
    /// <summary>
    /// Implements a list that uses an array of objects to store the elements.
    /// 
    /// This differs from a <see cref="System.Collections.Generic.List{T}"/> in that
    /// it not only grows as required but also shrinks if cleared with significant
    /// excess capacity. This makes it useful for component rendering, because
    /// components can be long-lived and re-render frequently, with the rendered size
    /// varying dramatically depending on the user's navigation in the app.
    /// </summary>
    internal class ArrayBuilder<T> : IDisposable
    {
        // The following fields are memory mapped to the WASM client. Do not re-order or use auto-properties.
        private T[] _items;
        private int _itemsInUse;

        private static readonly T[] Empty = Array.Empty<T>();
        private readonly ArrayPool<T> _arrayPool;
        private readonly int _minCapacity;
        private bool _disposed;

        /// <summary>
        /// Constructs a new instance of <see cref="ArrayBuilder{T}"/>.
        /// </summary>
        public ArrayBuilder(int minCapacity = 32, ArrayPool<T> arrayPool = null)
        {
            _arrayPool = arrayPool ?? ArrayPool<T>.Shared;
            _minCapacity = minCapacity;
            _items = Empty;
        }

        /// <summary>
        /// Gets the number of items.
        /// </summary>
        public int Count => _itemsInUse;

        /// <summary>
        /// Gets the underlying buffer.
        /// </summary>
        public T[] Buffer => _items;

        /// <summary>
        /// Appends a new item, automatically resizing the underlying array if necessary.
        /// </summary>
        /// <param name="item">The item to append.</param>
        /// <returns>The index of the appended item.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Just like System.Collections.Generic.List<T>
        public int Append(in T item)
        {
            if (_itemsInUse == _items.Length)
            {
                GrowBuffer(_items.Length * 2);
            }

            var indexOfAppendedItem = _itemsInUse++;
            _items[indexOfAppendedItem] = item;
            return indexOfAppendedItem;
        }

        internal int Append(T[] source, int startIndex, int length)
        {
            // Expand storage if needed. Using same doubling approach as would
            // be used if you inserted the items one-by-one.
            var requiredCapacity = _itemsInUse + length;
            if (_items.Length < requiredCapacity)
            {
                var candidateCapacity = Math.Max(_items.Length * 2, _minCapacity);
                while (candidateCapacity < requiredCapacity)
                {
                    candidateCapacity *= 2;
                }

                GrowBuffer(candidateCapacity);
            }

            Array.Copy(source, startIndex, _items, _itemsInUse, length);
            var startIndexOfAppendedItems = _itemsInUse;
            _itemsInUse += length;
            return startIndexOfAppendedItems;
        }

        /// <summary>
        /// Sets the supplied value at the specified index. The index must be within
        /// range for the array.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Overwrite(int index, in T value)
        {
            if (index > _itemsInUse)
            {
                ThrowIndexOutOfBoundsException();
            }

            _items[index] = value;
        }

        /// <summary>
        /// Removes the last item.
        /// </summary>
        public void RemoveLast()
        {
            if (_itemsInUse == 0)
            {
                ThrowIndexOutOfBoundsException();
            }

            _itemsInUse--;
            _items[_itemsInUse] = default; // Release to GC
        }

        /// <summary>
        /// Inserts the item at the specified index, moving the contents of the subsequent entries along by one.
        /// </summary>
        /// <param name="index">The index at which the value is to be inserted.</param>
        /// <param name="value">The value to insert.</param>
        public void InsertExpensive(int index, T value)
        {
            if (index > _itemsInUse)
            {
                ThrowIndexOutOfBoundsException();
            }

            // Same expansion logic as elsewhere
            if (_itemsInUse == _items.Length)
            {
                GrowBuffer(_items.Length * 2);
            }

            Array.Copy(_items, index, _items, index + 1, _itemsInUse - index);
            _itemsInUse++;

            _items[index] = value;
        }

        /// <summary>
        /// Marks the array as empty, also shrinking the underlying storage if it was
        /// not being used to near its full capacity.
        /// </summary>
        public void Clear()
        {
            ReturnBuffer();
            _items = Empty;
            _itemsInUse = 0;
        }

        private void GrowBuffer(int desiredCapacity)
        {
            // When we dispose, we set the count back to zero and return the array.
            //
            // If someone tries to do something that would require non-zero storage then
            // this is a use-after-free. Throwing here is an easy way to prevent that without
            // introducing overhead to every method.
            if (_disposed)
            {
                ThrowObjectDisposedException();
            }

            var newCapacity = Math.Max(desiredCapacity, _minCapacity);
            Debug.Assert(newCapacity > _items.Length);

            var newItems = _arrayPool.Rent(newCapacity);
            Array.Copy(_items, newItems, _itemsInUse);

            // Return the old buffer and start using the new buffer
            ReturnBuffer();
            _items = newItems;
        }

        private void ReturnBuffer()
        {
            if (!ReferenceEquals(_items, Empty))
            {
                // ArrayPool<>.Return with clearArray: true calls Array.Clear on the entire buffer.
                // In the most common case, _itemsInUse would be much smaller than _items.Length so we'll specifically clear that subset.
                Array.Clear(_items, 0, _itemsInUse);
                _arrayPool.Return(_items);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                ReturnBuffer();
                _items = Empty;
                _itemsInUse = 0;
            }
        }

        private static void ThrowIndexOutOfBoundsException()
        {
            throw new ArgumentOutOfRangeException("index");
        }

        private static void ThrowObjectDisposedException()
        {
            throw new ObjectDisposedException(objectName: null);
        }
    }
}
