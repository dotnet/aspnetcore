// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

internal static class SpecializedCollections
{
    public static IEnumerator<T> EmptyEnumerator<T>()
    {
        return Empty.Enumerator<T>.Instance;
    }

    public static IEnumerable<T> EmptyEnumerable<T>()
    {
        return Empty.List<T>.Instance;
    }

    public static ICollection<T> EmptyCollection<T>()
    {
        return Empty.List<T>.Instance;
    }

    public static IList<T> EmptyList<T>()
    {
        return Empty.List<T>.Instance;
    }

    public static IReadOnlyList<T> EmptyReadOnlyList<T>()
    {
        return Empty.List<T>.Instance;
    }

    private class Empty
    {
        internal class Enumerator<T> : Enumerator, IEnumerator<T>
        {
            public static new readonly IEnumerator<T> Instance = new Enumerator<T>();

            protected Enumerator()
            {
            }

            public new T Current => throw new InvalidOperationException();

            public void Dispose()
            {
            }
        }

        internal class Enumerator : IEnumerator
        {
            public static readonly IEnumerator Instance = new Enumerator();

            protected Enumerator()
            {
            }

            public object Current => throw new InvalidOperationException();

            public bool MoveNext()
            {
                return false;
            }

            public void Reset()
            {
                throw new InvalidOperationException();
            }
        }

        internal class Enumerable<T> : IEnumerable<T>
        {
            // PERF: cache the instance of enumerator.
            // accessing a generic static field is kinda slow from here,
            // but since empty enumerables are singletons, there is no harm in having
            // one extra instance field
            private readonly IEnumerator<T> _enumerator = Enumerator<T>.Instance;

            public IEnumerator<T> GetEnumerator()
            {
                return _enumerator;
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        internal class Collection<T> : Enumerable<T>, ICollection<T>
        {
            public static readonly ICollection<T> Instance = new Collection<T>();

            protected Collection()
            {
            }

            public void Add(T item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(T item)
            {
                return false;
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
            }

            public int Count => 0;

            public bool IsReadOnly => true;

            public bool Remove(T item)
            {
                throw new NotSupportedException();
            }
        }

        internal class List<T> : Collection<T>, IList<T>, IReadOnlyList<T>
        {
            public static new readonly List<T> Instance = new List<T>();

            protected List()
            {
            }

            public int IndexOf(T item)
            {
                return -1;
            }

            public void Insert(int index, T item)
            {
                throw new NotSupportedException();
            }

            public void RemoveAt(int index)
            {
                throw new NotSupportedException();
            }

            public T this[int index]
            {
                get
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                set
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}
