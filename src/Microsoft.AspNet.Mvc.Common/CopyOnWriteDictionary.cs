// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// A <see cref="IDictionary{TKey, TValue}"/> that defers creating a shallow copy of the source dictionary until
    ///  a mutative operation has been performed on it.
    /// </summary>
    internal class CopyOnWriteDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> _sourceDictionary;
        private readonly IEqualityComparer<TKey> _comparer;
        private IDictionary<TKey, TValue> _innerDictionary;

        public CopyOnWriteDictionary([NotNull] IDictionary<TKey, TValue> sourceDictionary,
                                     [NotNull] IEqualityComparer<TKey> comparer)
        {
            _sourceDictionary = sourceDictionary;
            _comparer = comparer;
        }

        private IDictionary<TKey, TValue> ReadDictionary
        {
            get
            {
                return _innerDictionary ?? _sourceDictionary;
            }
        }

        private IDictionary<TKey, TValue> WriteDictionary
        {
            get
            {
                if (_innerDictionary == null)
                {
                    _innerDictionary = new Dictionary<TKey, TValue>(_sourceDictionary,
                                                                    _comparer);
                }

                return _innerDictionary;
            }
        }

        public virtual ICollection<TKey> Keys
        {
            get
            {
                return ReadDictionary.Keys;
            }
        }

        public virtual ICollection<TValue> Values
        {
            get
            {
                return ReadDictionary.Values;
            }
        }

        public virtual int Count
        {
            get
            {
                return ReadDictionary.Count;
            }
        }

        public virtual bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public virtual TValue this[[NotNull] TKey key]
        {
            get
            {
                return ReadDictionary[key];
            }
            set
            {
                WriteDictionary[key] = value;
            }
        }

        public virtual bool ContainsKey([NotNull] TKey key)
        {
            return ReadDictionary.ContainsKey(key);
        }

        public virtual void Add([NotNull] TKey key, TValue value)
        {
            WriteDictionary.Add(key, value);
        }

        public virtual bool Remove([NotNull] TKey key)
        {
            return WriteDictionary.Remove(key);
        }

        public virtual bool TryGetValue([NotNull] TKey key, out TValue value)
        {
            return ReadDictionary.TryGetValue(key, out value);
        }

        public virtual void Add(KeyValuePair<TKey, TValue> item)
        {
            WriteDictionary.Add(item);
        }

        public virtual void Clear()
        {
            WriteDictionary.Clear();
        }

        public virtual bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ReadDictionary.Contains(item);
        }

        public virtual void CopyTo([NotNull] KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ReadDictionary.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return WriteDictionary.Remove(item);
        }

        public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return ReadDictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}