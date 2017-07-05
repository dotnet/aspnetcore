// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    public sealed class ItemCollection : ICollection<KeyValuePair<object, object>>
    {
        private readonly Dictionary<object, object> _inner;

        public ItemCollection()
        {
            _inner = new Dictionary<object, object>();
        }

        public object this[object key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }
                
                _inner.TryGetValue(key, out var value);
                return value;
            }
            set
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                _inner[key] = value;
            }
        }

        public int Count => _inner.Count;

        public bool IsReadOnly => _inner != null;

        int ICollection<KeyValuePair<object, object>>.Count => throw new NotImplementedException();

        bool ICollection<KeyValuePair<object, object>>.IsReadOnly => throw new NotImplementedException();

        public void Add(KeyValuePair<object, object> item)
        {
            if (item.Key == null)
            {
                throw new ArgumentException(Resources.KeyMustNotBeNull, nameof(item));
            }

            ((ICollection<KeyValuePair<object, object>>)_inner).Add(item);
        }

        public void Add(object key, object value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            _inner.Add(key, value);
        }

        public void Clear()
        {
            _inner.Clear();
        }

        public bool Contains(KeyValuePair<object, object> item)
        {
            if (item.Key == null)
            {
                throw new ArgumentException(Resources.KeyMustNotBeNull, nameof(item));
            }

            return ((ICollection<KeyValuePair<object, object>>)_inner).Contains(item);
        }

        public void CopyTo(KeyValuePair<object, object>[] array, int arrayIndex)
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

            ((ICollection<KeyValuePair<object, object>>)_inner).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<object, object>> GetEnumerator()
        {
            return _inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Remove(KeyValuePair<object, object> item)
        {
            if (item.Key == null)
            {
                throw new ArgumentException(Resources.KeyMustNotBeNull, nameof(item));
            }

            return ((ICollection<KeyValuePair<object, object>>)_inner).Remove(item);
        }
    }
}
