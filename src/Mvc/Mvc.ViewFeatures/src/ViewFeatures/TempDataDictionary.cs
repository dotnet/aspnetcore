// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    /// <inheritdoc />
    public class TempDataDictionary : ITempDataDictionary
    {
        // Perf: Everything here is lazy because the TempDataDictionary is frequently created and passed around
        // without being manipulated.
        private Dictionary<string, object> _data;
        private bool _loaded;
        private readonly ITempDataProvider _provider;
        private readonly HttpContext _context;
        private HashSet<string> _initialKeys;
        private HashSet<string> _retainedKeys;

        /// <summary>
        /// Initializes a new instance of the <see cref="TempDataDictionary"/> class.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/>.</param>
        /// <param name="provider">The <see cref="ITempDataProvider"/> used to Load and Save data.</param>
        public TempDataDictionary(HttpContext context, ITempDataProvider provider)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            _provider = provider;
            _loaded = false;
            _context = context;
        }

        public int Count
        {
            get
            {
                Load();
                return _data.Count;
            }
        }

        public ICollection<string> Keys
        {
            get
            {
                Load();
                return _data.Keys;
            }
        }

        public ICollection<object> Values
        {
            get
            {
                Load();
                return _data.Values;
            }
        }

        bool ICollection<KeyValuePair<string, object>>.IsReadOnly
        {
            get
            {
                Load();
                return ((ICollection<KeyValuePair<string, object>>)_data).IsReadOnly;
            }
        }

        public object this[string key]
        {
            get
            {
                Load();
                object value;
                if (TryGetValue(key, out value))
                {
                    // Mark the key for deletion since it is read.
                    _initialKeys.Remove(key);
                    return value;
                }
                return null;
            }
            set
            {
                Load();
                _data[key] = value;
                _initialKeys.Add(key);
            }
        }

        /// <inheritdoc />
        public void Keep()
        {
            // if the data is not loaded, we can assume none of it has been read
            // and so silently return.
            if (!_loaded)
            {
                return;
            }

            _retainedKeys.Clear();
            _retainedKeys.UnionWith(_data.Keys);
        }

        /// <inheritdoc />
        public void Keep(string key)
        {
            Load();
            _retainedKeys.Add(key);
        }

        /// <inheritdoc />
        public void Load()
        {
            if (_loaded)
            {
                return;
            }

            var providerDictionary = _provider.LoadTempData(_context);
            _data = (providerDictionary != null)
                ? new Dictionary<string, object>(providerDictionary, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            _initialKeys = new HashSet<string>(_data.Keys, StringComparer.OrdinalIgnoreCase);
            _retainedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _loaded = true;
        }

        /// <inheritdoc />
        public void Save()
        {
            if (!_loaded)
            {
                return;
            }

            // Because it is not possible to delete while enumerating, a copy of the keys must be taken.
            // Use the size of the dictionary as an upper bound to avoid creating more than one copy of the keys.
            var removeCount = 0;
            var keys = new string[_data.Count];
            foreach (var entry in _data)
            {
                if (!_initialKeys.Contains(entry.Key) && !_retainedKeys.Contains(entry.Key))
                {
                    keys[removeCount] = entry.Key;
                    removeCount++;
                }
            }
            for (var i = 0; i < removeCount; i++)
            {
                _data.Remove(keys[i]);
            }

            _provider.SaveTempData(_context, _data);
        }

        /// <inheritdoc />
        public object Peek(string key)
        {
            Load();
            object value;
            _data.TryGetValue(key, out value);
            return value;
        }

        public void Add(string key, object value)
        {
            Load();
            _data.Add(key, value);
            _initialKeys.Add(key);
        }

        public void Clear()
        {
            Load();
            _data.Clear();
            _retainedKeys.Clear();
            _initialKeys.Clear();
        }

        public bool ContainsKey(string key)
        {
            Load();
            return _data.ContainsKey(key);
        }

        public bool ContainsValue(object value)
        {
            Load();
            return _data.ContainsValue(value);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            Load();
            return new TempDataDictionaryEnumerator(this);
        }

        public bool Remove(string key)
        {
            Load();
            _retainedKeys.Remove(key);
            _initialKeys.Remove(key);
            return _data.Remove(key);
        }

        public bool TryGetValue(string key, out object value)
        {
            Load();
            // Mark the key for deletion since it is read.
            _initialKeys.Remove(key);
            return _data.TryGetValue(key, out value);
        }

        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int index)
        {
            Load();
            ((ICollection<KeyValuePair<string, object>>)_data).CopyTo(array, index);
        }

        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> keyValuePair)
        {
            Load();
            _initialKeys.Add(keyValuePair.Key);
            ((ICollection<KeyValuePair<string, object>>)_data).Add(keyValuePair);
        }

        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> keyValuePair)
        {
            Load();
            return ((ICollection<KeyValuePair<string, object>>)_data).Contains(keyValuePair);
        }

        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> keyValuePair)
        {
            Load();
            _initialKeys.Remove(keyValuePair.Key);
            return ((ICollection<KeyValuePair<string, object>>)_data).Remove(keyValuePair);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            Load();
            return new TempDataDictionaryEnumerator(this);
        }

        private sealed class TempDataDictionaryEnumerator : IEnumerator<KeyValuePair<string, object>>
        {
            private readonly IEnumerator<KeyValuePair<string, object>> _enumerator;
            private readonly TempDataDictionary _tempData;

            public TempDataDictionaryEnumerator(TempDataDictionary tempData)
            {
                _tempData = tempData;
                _enumerator = _tempData._data.GetEnumerator();
            }

            public KeyValuePair<string, object> Current
            {
                get
                {
                    var kvp = _enumerator.Current;
                    // Mark the key for deletion since it is read.
                    _tempData._initialKeys.Remove(kvp.Key);
                    return kvp;
                }
            }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                _enumerator.Reset();
            }

            void IDisposable.Dispose()
            {
                _enumerator.Dispose();
            }
        }
    }
}