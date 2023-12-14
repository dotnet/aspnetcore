// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(CopyOnWriteList<>.CopyOnWriteListDebugView))]
internal sealed class CopyOnWriteList<T> : IList<T>
{
    private readonly IReadOnlyList<T> _source;
    private List<T>? _copy;

    public CopyOnWriteList(IReadOnlyList<T> source)
    {
        _source = source;
    }

    private IReadOnlyList<T> Readable => _copy ?? _source;

    private List<T> Writable
    {
        get
        {
            if (_copy == null)
            {
                _copy = new List<T>(_source);
            }

            return _copy;
        }
    }

    public T this[int index]
    {
        get => Readable[index];
        set => Writable[index] = value;
    }

    public int Count => Readable.Count;

    public bool IsReadOnly => false;

    public void Add(T item)
    {
        Writable.Add(item);
    }

    public void Clear()
    {
        Writable.Clear();
    }

    public bool Contains(T item)
    {
        foreach (var obj in Readable)
        {
            if (object.Equals(obj, item))
            {
                return true;
            }
        }

        return false;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        foreach (var item in Readable)
        {
            array[arrayIndex++] = item;
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        return Readable.GetEnumerator();
    }

    public int IndexOf(T item)
    {
        var i = 0;
        foreach (var obj in Readable)
        {
            if (object.Equals(obj, item))
            {
                return i;
            }

            i++;
        }

        return -1;
    }

    public void Insert(int index, T item)
    {
        Writable.Insert(index, item);
    }

    public bool Remove(T item)
    {
        return Writable.Remove(item);
    }

    public void RemoveAt(int index)
    {
        Writable.RemoveAt(index);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private sealed class CopyOnWriteListDebugView(CopyOnWriteList<T> collection)
    {
        private readonly CopyOnWriteList<T> _collection = collection;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => _collection.ToArray();
    }
}
