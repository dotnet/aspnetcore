// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Routing;

// Simple list wrapper that avoids array allocations if the number of items is below a threshold.
struct DynamicArray<T>
{
    // 4 is a good default capacity here because that leaves enough space for area/controller/action/id
    private const int DefaultCapacity = 4;

    private InlineArray<T> _inlineArray;
    private T[]? _array;
    private int _size;

    public readonly int Length => _size;

    public DynamicArray()
    {
    }

    public DynamicArray(ReadOnlySpan<T> values)
    {
        _size = values.Length;

        if (values.Length > DefaultCapacity)
        {
            _array = values.ToArray();
        }
        else
        {
            values.CopyTo(_inlineArray);
        }
    }

    public DynamicArray(T[] array, int count)
    {
        _array = array;
        _size = count;
    }

    public T this[int index]
    {
        get => (_array ?? (Span<T>)_inlineArray)[index];
        set => (_array ?? (Span<T>)_inlineArray)[index] = value;
    }

    public readonly ReadOnlySpan<T> AsSpan()
    {
        if (_array is null)
        {
            return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<InlineArray<T>, T>(ref Unsafe.AsRef(in _inlineArray)), _size);
        }
        return _array.AsSpan(0, _size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        Span<T> span = _array ?? (Span<T>)_inlineArray;
        int size = _size;
        if ((uint)size < (uint)span.Length)
        {
            _size = size + 1;
            span[size] = item;
        }
        else
        {
            AddWithResize(item);
        }
    }

    // Non-inline from List.Add to improve its code quality as uncommon path
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AddWithResize(T item)
    {
        // Debug.Assert(_size == _items.Length);
        Grow();
        int size = _size;
        _size = size + 1;
        _array![size] = item;
    }

    private void Grow()
    {
        if (_array is null)
        {
            _array = new T[DefaultCapacity * 2];
            ((Span<T>)_inlineArray).CopyTo(_array);
        }
        else
        {
            var newArray = new T[_array.Length * 2];
            _array.AsSpan().CopyTo(newArray);
            _array = newArray;
        }
    }

    public void RemoveAt(int index)
    {
        if ((uint)index >= (uint)_size)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(index, _size);
        }

        _size--;

        Span<T> span = _array ?? (Span<T>)_inlineArray;

        if (index < _size)
        {
            span.Slice(index + 1, _size - index).CopyTo(span.Slice(index));
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            span[_size] = default!;
        }
    }

    public void Clear()
    {
        Span<T> span = _array ?? (Span<T>)_inlineArray;

        span.Clear();

        _size = 0;
        _array = null;
    }

    [InlineArray(DefaultCapacity)]
    struct InlineArray<TItem>
    {
#pragma warning disable CA1823 // Avoid unused private fields
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0051 // Remove unused private members
        private TItem _item0;
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore IDE0044 // Add readonly modifier
#pragma warning restore CA1823 // Avoid unused private fields
    }
}
