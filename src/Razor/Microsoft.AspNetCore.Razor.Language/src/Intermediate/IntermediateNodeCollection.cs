// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public sealed class IntermediateNodeCollection : IList<IntermediateNode>
{
    public static readonly IntermediateNodeCollection ReadOnly = new IntermediateNodeCollection(new List<IntermediateNode>().AsReadOnly());

    private readonly IList<IntermediateNode> _inner;

    public IntermediateNodeCollection()
        : this(new List<IntermediateNode>())
    {
    }

    private IntermediateNodeCollection(IList<IntermediateNode> inner)
    {
        _inner = inner;
    }

    public IntermediateNode this[int index]
    {
        get
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _inner[index];
        }
        set
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            _inner[index] = value;
        }
    }

    public int Count => _inner.Count;

    public bool IsReadOnly => _inner.IsReadOnly;

    public void Add(IntermediateNode item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        _inner.Add(item);
    }

    public void AddRange(IEnumerable<IntermediateNode> items)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        foreach (var item in items)
        {
            _inner.Add(item);
        }
    }

    public void AddRange(IntermediateNodeCollection items)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        var count = items.Count;
        for (var i = 0; i < count; i++)
        {
            _inner.Add(items[i]);
        }
    }

    public void Clear()
    {
        _inner.Clear();
    }

    public bool Contains(IntermediateNode item)
    {
        return _inner.Contains(item);
    }

    public void CopyTo(IntermediateNode[] array, int arrayIndex)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        if (arrayIndex < 0 || arrayIndex > array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }
        else if (array.Length - arrayIndex < Count)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }

        _inner.CopyTo(array, arrayIndex);
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator<IntermediateNode> IEnumerable<IntermediateNode>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int IndexOf(IntermediateNode item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        return _inner.IndexOf(item);
    }

    public void Insert(int index, IntermediateNode item)
    {
        if (index < 0 || index > Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        _inner.Insert(index, item);
    }

    public bool Remove(IntermediateNode item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        return _inner.Remove(item);
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        _inner.RemoveAt(index);
    }

    public struct Enumerator : IEnumerator<IntermediateNode>
    {
        private readonly IList<IntermediateNode> _items;
        private int _index;

        public Enumerator(IntermediateNodeCollection collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            _items = collection._inner;
            _index = -1;
        }

        public IntermediateNode Current
        {
            get
            {
                if (_index < 0 || _index >= _items.Count)
                {
                    return null;
                }

                return _items[_index];
            }
        }

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            _index++;
            return _index < _items.Count;
        }

        public void Reset()
        {
            _index = -1;
        }
    }
}
