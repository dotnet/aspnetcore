// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Blazor.RenderTree
{
    /// <summary>
    /// Implements a list that uses an array of objects to store the elements.
    /// </summary>
    public class ArrayBuilder<T>
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Just like System.Collections.Generic.List<T>
        public void Append(in T item)
        {
            if (_itemsInUse == _items.Length)
            {
                SetCapacity(_itemsInUse * 2, preserveContents: true);
            }

            _items[_itemsInUse++] = item;
        }

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
            else
            {
                Array.Clear(_items, 0, _itemsInUse); // Release to GC
            }
        }

        /// <summary>
        /// Produces an <see cref="ArrayRange{T}"/> structure describing the current contents.
        /// </summary>
        /// <returns>The <see cref="ArrayRange{T}"/>.</returns>
        public ArrayRange<T> ToRange()
            => new ArrayRange<T>(_items, _itemsInUse);

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
