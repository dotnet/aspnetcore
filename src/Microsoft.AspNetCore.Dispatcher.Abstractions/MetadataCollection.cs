// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class MetadataCollection : IReadOnlyList<object>
    {
        private readonly object[] _items;

        public MetadataCollection()
        {
            _items = Array.Empty<object>();
        }

        public MetadataCollection(IEnumerable<object> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            _items = items.ToArray();
        }

        public object this[int index] => _items[index];

        public int Count => _items.Length;

        public T GetMetadata<T>() where T : class
        {
            for (var i = _items.Length -1; i >= 0; i--)
            {
                var item = _items[i] as T;
                if (item !=null)
                {
                    return item;
                }
            }

            return default;
        }

        public IEnumerable<T> GetOrderedMetadata<T>() where T : class
        {
            for (var i = 0; i < _items.Length; i++)
            {
                var item = _items[i] as T;
                if (item != null)
                {
                    yield return item;
                }
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<object> IEnumerable<object>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<object>
        {
            private object[] _items;
            private int _index;
            private object _current;

            internal Enumerator(MetadataCollection collection)
            {
                _items = collection._items;
                _index = 0;
                _current = null;
            }

            public object Current => _current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_index < _items.Length)
                {
                    _current = _items[_index++];
                    return true;
                }

                _current = null;
                return false;
            }

            public void Reset()
            {
                _index = 0;
                _current = null;
            }
        }
    }
}
