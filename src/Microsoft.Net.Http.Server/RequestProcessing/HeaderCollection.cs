// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Net.Http.Server
{
    public class HeaderCollection : IDictionary<string, StringValues>
    {
        public HeaderCollection()
            : this(new Dictionary<string, StringValues>(4, StringComparer.OrdinalIgnoreCase))
        {
        }

        public HeaderCollection(IDictionary<string, StringValues> store)
        {
            Store = store;
        }

        private IDictionary<string, StringValues> Store { get; set; }

        // Readonly after the response has been started.
        public bool IsReadOnly { get; internal set; }

        public StringValues this[string key]
        {
            get
            {
                StringValues values;
                return TryGetValue(key, out values) ? values : StringValues.Empty;
            }
            set
            {
                ThrowIfReadOnly();
                if (StringValues.IsNullOrEmpty(value))
                {
                    Remove(key);
                }
                else
                {
                    ValidateHeaderCharacters(key);
                    ValidateHeaderCharacters(value);
                    Store[key] = value;
                }
            }
        }

        StringValues IDictionary<string, StringValues>.this[string key]
        {
            get { return Store[key]; }
            set
            {
                ThrowIfReadOnly();
                ValidateHeaderCharacters(key);
                ValidateHeaderCharacters(value);
                Store[key] = value;
            }
        }

        public int Count
        {
            get { return Store.Count; }
        }

        public ICollection<string> Keys
        {
            get { return Store.Keys; }
        }

        public ICollection<StringValues> Values
        {
            get { return Store.Values; }
        }

        public void Add(KeyValuePair<string, StringValues> item)
        {
            ThrowIfReadOnly();
            ValidateHeaderCharacters(item.Key);
            ValidateHeaderCharacters(item.Value);
            Store.Add(item);
        }

        public void Add(string key, StringValues value)
        {
            ThrowIfReadOnly();
            ValidateHeaderCharacters(key);
            ValidateHeaderCharacters(value);
            Store.Add(key, value);
        }

        public void Append(string key, string value)
        {
            ThrowIfReadOnly();
            ValidateHeaderCharacters(key);
            ValidateHeaderCharacters(value);
            StringValues values;
            Store.TryGetValue(key, out values);
            Store[key] = StringValues.Concat(values, value);
        }

        public void Clear()
        {
            ThrowIfReadOnly();
            Store.Clear();
        }

        public bool Contains(KeyValuePair<string, StringValues> item)
        {
            return Store.Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return Store.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex)
        {
            Store.CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
        {
            return Store.GetEnumerator();
        }

        public IEnumerable<string> GetValues(string key)
        {
            StringValues values;
            if (Store.TryGetValue(key, out values))
            {
                return HeaderParser.SplitValues(values);
            }
            return HeaderParser.Empty;
        }

        public bool Remove(KeyValuePair<string, StringValues> item)
        {
            ThrowIfReadOnly();
            return Store.Remove(item);
        }

        public bool Remove(string key)
        {
            ThrowIfReadOnly();
            return Store.Remove(key);
        }

        public bool TryGetValue(string key, out StringValues value)
        {
            return Store.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void ThrowIfReadOnly()
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException("The response headers cannot be modified because the response has already started.");
            }
        }

        public static void ValidateHeaderCharacters(StringValues headerValues)
        {
            foreach (var value in headerValues)
            {
                ValidateHeaderCharacters(value);
            }
        }

        public static void ValidateHeaderCharacters(string headerCharacters)
        {
            if (headerCharacters != null)
            {
                foreach (var ch in headerCharacters)
                {
                    if (ch < 0x20)
                    {
                        throw new InvalidOperationException(string.Format("Invalid control character in header: 0x{0:X2}", (byte)ch));
                    }
                }
            }
        }
    }
}