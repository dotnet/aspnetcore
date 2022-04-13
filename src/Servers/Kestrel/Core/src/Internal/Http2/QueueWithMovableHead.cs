// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

// This is a normal queue with a twist, we have the ability to set the head to a different start index
internal class QueueWithMovableHead<T>
{
    private T[] _array;
    private int _head;       // The index from which to dequeue if the queue isn't empty.
    private int _tail;       // The index at which to enqueue if the queue isn't full.
    private int _size;       // Number of elements.

    public int Count => _size;

    // Creates a queue with room for capacity objects. The default initial
    // capacity and grow factor are used.
    public QueueWithMovableHead()
    {
        _array = Array.Empty<T>();
    }

    public int Enqueue(T item)
    {
        if (_size == _array.Length)
        {
            Grow(_size + 1);
        }

        var pos = _tail;
        _array[_tail] = item;
        MoveNext(ref _tail, _array.Length);
        _size++;
        return pos;
    }

    // Sets the index of the head of the queue
    public void SetHead(int head)
    {
        _head = head;
    }

    public bool TryDequeue([MaybeNullWhen(false)] out T result)
    {
        var head = _head;
        var array = _array;

        if (_size == 0)
        {
            result = default!;
            return false;
        }

        result = array[head];
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            array[head] = default!;
        }
        MoveNext(ref _head, _head < _tail ? _tail : _array.Length);
        _size--;

        if (_size == 0)
        {
            // Restore the invarint that head = tail on empty queue
            _head = _tail;
        }

        return true;
    }

    private static void MoveNext(ref int index, int end)
    {
        // It is tempting to use the remainder operator here but it is actually much slower
        // than a simple comparison and a rarely taken branch.
        // JIT produces better code than with ternary operator ?:
        var tmp = index + 1;
        if (tmp == end)
        {
            tmp = 0;
        }
        index = tmp;
    }

    private void Grow(int capacity)
    {
        Debug.Assert(_array.Length < capacity);

        const int GrowFactor = 2;
        const int MinimumGrow = 4;

        var newcapacity = GrowFactor * _array.Length;

        // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
        // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
        if ((uint)newcapacity > Array.MaxLength)
        {
            newcapacity = Array.MaxLength;
        }

        // Ensure minimum growth is respected.
        newcapacity = Math.Max(newcapacity, _array.Length + MinimumGrow);

        // If the computed capacity is still less than specified, set to the original argument.
        // Capacities exceeding Array.MaxLength will be surfaced as OutOfMemoryException by Array.Resize.
        if (newcapacity < capacity)
        {
            newcapacity = capacity;
        }

        SetCapacity(newcapacity);
    }

    private void SetCapacity(int capacity)
    {
        var newarray = new T[capacity];
        if (_size > 0)
        {
            if (_head < _tail)
            {
                Array.Copy(_array, _head, newarray, 0, _size);
            }
            else
            {
                Array.Copy(_array, _head, newarray, 0, _array.Length - _head);
                Array.Copy(_array, 0, newarray, _array.Length - _head, _tail);
            }
        }

        _array = newarray;
        _head = 0;
        _tail = (_size == capacity) ? 0 : _size;
    }
}
