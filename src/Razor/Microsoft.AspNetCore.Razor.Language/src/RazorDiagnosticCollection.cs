// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RazorDiagnosticCollection : IList<RazorDiagnostic>
{
    private readonly List<RazorDiagnostic> _inner;

    public RazorDiagnosticCollection()
    {
        _inner = new List<RazorDiagnostic>();
    }

    public RazorDiagnostic this[int index]
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

    public bool IsReadOnly => _inner != null;

    public void Add(RazorDiagnostic item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        _inner.Add(item);
    }

    public void AddRange(RazorDiagnosticCollection items)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        _inner.AddRange(items);
    }

    public void AddRange(IEnumerable<RazorDiagnostic> items)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        _inner.AddRange(items);
    }

    public void Clear()
    {
        _inner.Clear();
    }

    public bool Contains(RazorDiagnostic item)
    {
        return _inner.Contains(item);
    }

    public void CopyTo(RazorDiagnostic[] array, int arrayIndex)
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

    IEnumerator<RazorDiagnostic> IEnumerable<RazorDiagnostic>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int IndexOf(RazorDiagnostic item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        return _inner.IndexOf(item);
    }

    public void Insert(int index, RazorDiagnostic item)
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

    public bool Remove(RazorDiagnostic item)
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

    public struct Enumerator : IEnumerator<RazorDiagnostic>
    {
        private readonly IList<RazorDiagnostic> _items;
        private int _index;

        public Enumerator(RazorDiagnosticCollection collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            _items = collection._inner;
            _index = -1;
        }

        public RazorDiagnostic Current
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
