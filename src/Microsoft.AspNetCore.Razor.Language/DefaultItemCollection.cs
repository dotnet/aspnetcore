// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultItemCollection : ItemCollection
    {
        private readonly Dictionary<object, object> _items;

        public DefaultItemCollection()
        {
            _items = new Dictionary<object, object>();
        }

        public override object this[object key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                object value;
                _items.TryGetValue(key, out value);
                return value;
            }

            set
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                _items[key] = value;
            }
        }

        public override int Count => _items.Count;

        public override bool IsReadOnly => false;

        public override void Add(KeyValuePair<object, object> item)
        {
            if (item.Key == null)
            {
                throw new ArgumentException(Resources.KeyMustNotBeNull, nameof(item));
            }
            
            ((ICollection<KeyValuePair<object, object>>)_items).Add(item);
        }

        public override void Clear()
        {
            _items.Clear();
        }

        public override bool Contains(KeyValuePair<object, object> item)
        {
            if (item.Key == null)
            {
                throw new ArgumentException(Resources.KeyMustNotBeNull, nameof(item));
            }
            
            return ((ICollection<KeyValuePair<object, object>>)_items).Contains(item);
        }

        public override void CopyTo(KeyValuePair<object, object>[] array, int arrayIndex)
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

            ((ICollection<KeyValuePair<object, object>>)_items).CopyTo(array, arrayIndex);
        }

        public override IEnumerator<KeyValuePair<object, object>> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public override bool Remove(KeyValuePair<object, object> item)
        {
            if (item.Key == null)
            {
                throw new ArgumentException(Resources.KeyMustNotBeNull, nameof(item));
            }
            
            return ((ICollection<KeyValuePair<object, object>>)_items).Remove(item);
        }
    }
}
