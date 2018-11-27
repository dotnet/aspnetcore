// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Components.RenderTree
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
    internal class ArrayBuilder<T>
    {
        private const int MinCapacity = 10;
        private T[] _items;
        private int _itemsInUse;

        /// <summary>
        /// Constructs a new instance of <see cref="ArrayBuilder{T}"/>.
        /// </summary>
        public ArrayBuilder() : this(MinCapacity)
        {
        }

        /// <summary>
        /// Constructs a new instance of <see cref="ArrayBuilder{T}"/>.
        /// </summary>
        public ArrayBuilder(int capacity)
        {
            _items = new T[capacity < MinCapacity ? MinCapacity : capacity];
            _itemsInUse = 0;
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
                SetCapacity(_items.Length * 2, preserveContents: true);
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
                var candidateCapacity = _items.Length * 2;
                while (candidateCapacity < requiredCapacity)
                {
                    candidateCapacity *= 2;
                }

                SetCapacity(candidateCapacity, preserveContents: true);
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
            => _items[index] = value;

        /// <summary>
        /// Removes the last item.
        /// </summary>
        public void RemoveLast()
        {
            _itemsInUse--;
            _items[_itemsInUse] = default(T); // Release to GC
        }

        /// <summary>
        /// Marks the array as empty, also shrinking the underlying storage if it was
        /// not being used to near its full capacity.
        /// </summary>
        public void Clear()
        {
            var previousItemsInUse = _itemsInUse;
            _itemsInUse = 0;

            if (_items.Length > previousItemsInUse * 1.5)
            {
                SetCapacity((previousItemsInUse + _items.Length) / 2, preserveContents: false);
            }
            else if (previousItemsInUse > 0)
            {
                Array.Clear(_items, 0, previousItemsInUse); // Release to GC
            }
        }

        /// <summary>
        /// Produces an <see cref="ArrayRange{T}"/> structure describing the current contents.
        /// </summary>
        /// <returns>The <see cref="ArrayRange{T}"/>.</returns>
        public ArrayRange<T> ToRange()
            => new ArrayRange<T>(_items, _itemsInUse);

        /// <summary>
        /// Produces an <see cref="ArraySegment{T}"/> structure describing the selected contents.
        /// </summary>
        /// <param name="fromIndexInclusive">The index of the first item in the segment.</param>
        /// <param name="toIndexExclusive">One plus the index of the last item in the segment.</param>
        /// <returns>The <see cref="ArraySegment{T}"/>.</returns>
        public ArraySegment<T> ToSegment(int fromIndexInclusive, int toIndexExclusive)
            => new ArraySegment<T>(_items, fromIndexInclusive, toIndexExclusive - fromIndexInclusive);

        private void SetCapacity(int desiredCapacity, bool preserveContents)
        {
            if (desiredCapacity < _itemsInUse)
            {
                throw new ArgumentOutOfRangeException(nameof(desiredCapacity), $"The value cannot be less than {nameof(Count)}");
            }

            var newCapacity = desiredCapacity < MinCapacity ? MinCapacity : desiredCapacity;
            if (newCapacity != _items.Length)
            {
                var newItems = new T[newCapacity];

                if (preserveContents)
                {
                    Array.Copy(_items, newItems, _itemsInUse);
                }

                _items = newItems;
            }
            else if (!preserveContents)
            {
                Array.Clear(_items, 0, _items.Length);
            }
        }
    }
}
