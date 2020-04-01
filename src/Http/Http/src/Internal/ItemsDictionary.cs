// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Http
{
    internal class ItemsDictionary : IDictionary<object, object>
    {
        private IDictionary<object, object> _items;

        public ItemsDictionary()
        {}

        public ItemsDictionary(IDictionary<object, object> items)
        {
            _items = items;
        }

        public IDictionary<object, object> Items => this;

        // Replace the indexer with one that returns null for missing values
        object IDictionary<object, object>.this[object key]
        {
            get
            {
                if (_items != null && _items.TryGetValue(key, out var value))
                {
                    return value;
                }
                return null;
            }
            set
            {
                EnsureDictionary();
                _items[key] = value;
            }
        }

        void IDictionary<object, object>.Add(object key, object value)
        {
            EnsureDictionary();
            _items.Add(key, value);
        }

        bool IDictionary<object, object>.ContainsKey(object key)
            => _items != null && _items.ContainsKey(key);

        ICollection<object> IDictionary<object, object>.Keys
        {
            get
            {
                if (_items == null)
                {
                    return EmptyDictionary.Dictionary.Keys;
                }

                return _items.Keys;
            }
        }

        bool IDictionary<object, object>.Remove(object key)
            => _items != null && _items.Remove(key);

        bool IDictionary<object, object>.TryGetValue(object key, out object value)
        {
            value = null;
            return _items != null && _items.TryGetValue(key, out value);
        }

        ICollection<object> IDictionary<object, object>.Values
        {
            get
            {
                if (_items == null)
                {
                    return EmptyDictionary.Dictionary.Values;
                }

                return _items.Values;
            }
        }

        void ICollection<KeyValuePair<object, object>>.Add(KeyValuePair<object, object> item)
        {
            EnsureDictionary();
            _items.Add(item);
        }

        void ICollection<KeyValuePair<object, object>>.Clear() => _items?.Clear();

        bool ICollection<KeyValuePair<object, object>>.Contains(KeyValuePair<object, object> item)
            => _items != null && _items.Contains(item);

        void ICollection<KeyValuePair<object, object>>.CopyTo(KeyValuePair<object, object>[] array, int arrayIndex)
        {
            if (_items == null)
            {
                //Delegate to Empty Dictionary to do the argument checking.
                EmptyDictionary.Collection.CopyTo(array, arrayIndex);
            }

            _items?.CopyTo(array, arrayIndex);
        }

        int ICollection<KeyValuePair<object, object>>.Count => _items?.Count ?? 0;

        bool ICollection<KeyValuePair<object, object>>.IsReadOnly => _items?.IsReadOnly ?? false;

        bool ICollection<KeyValuePair<object, object>>.Remove(KeyValuePair<object, object> item)
        {
            if (_items == null)
            {
                return false;
            }

            if (_items.TryGetValue(item.Key, out var value) && Equals(item.Value, value))
            {
                return _items.Remove(item.Key);
            }
            return false;
        }

        private void EnsureDictionary()
        {
            if (_items == null)
            {
                _items = new Dictionary<object, object>();
            }
        }

        IEnumerator<KeyValuePair<object, object>> IEnumerable<KeyValuePair<object, object>>.GetEnumerator()
            => _items?.GetEnumerator() ?? EmptyEnumerator.Instance;

        IEnumerator IEnumerable.GetEnumerator() => _items?.GetEnumerator() ?? EmptyEnumerator.Instance;

        private class EmptyEnumerator : IEnumerator<KeyValuePair<object, object>>
        {
            // In own class so only initalized if GetEnumerator is called on an empty ItemsDictionary
            public readonly static IEnumerator<KeyValuePair<object, object>> Instance = new EmptyEnumerator();
            public KeyValuePair<object, object> Current => default;

            object IEnumerator.Current => null;

            public void Dispose()
            { }

            public bool MoveNext() => false;

            public void Reset()
            { }
        }

        private static class EmptyDictionary
        {
            // In own class so only initalized if CopyTo is called on an empty ItemsDictionary
            public readonly static IDictionary<object, object> Dictionary = new Dictionary<object, object>();
            public static ICollection<KeyValuePair<object, object>> Collection => Dictionary;
        }
    }
}
