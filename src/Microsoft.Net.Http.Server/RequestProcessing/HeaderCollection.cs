// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Net.Http.Server
{
    public class HeaderCollection : IDictionary<string, string[]>
    {
        public HeaderCollection()
            : this(new Dictionary<string, string[]>(4, StringComparer.OrdinalIgnoreCase))
        {
        }

        public HeaderCollection(IDictionary<string, string[]> store)
        {
            Store = store;
        }

        private IDictionary<string, string[]> Store { get; set; }

        // Readonly after the response has been sent.
        internal bool Sent { get; set; }

        public string this[string key]
        {
            get { return Get(key); }
            set
            {
                ThrowIfSent();
                if (string.IsNullOrEmpty(value))
                {
                    Remove(key);
                }
                else
                {
                    Set(key, value);
                }
            }
        }

        string[] IDictionary<string, string[]>.this[string key]
        {
            get { return Store[key]; }
            set
            {
                ThrowIfSent();
                Store[key] = value;
            }
        }

        public int Count
        {
            get { return Store.Count; }
        }

        public bool IsReadOnly
        {
            get { return Sent; }
        }

        public ICollection<string> Keys
        {
            get { return Store.Keys; }
        }

        public ICollection<string[]> Values
        {
            get { return Store.Values; }
        }

        public void Add(KeyValuePair<string, string[]> item)
        {
            ThrowIfSent();
            Store.Add(item);
        }

        public void Add(string key, string[] value)
        {
            ThrowIfSent();
            Store.Add(key, value);
        }

        public void Append(string key, string value)
        {
            ThrowIfSent();
            string[] values;
            if (Store.TryGetValue(key, out values))
            {
                var newValues = new string[values.Length + 1];
                Array.Copy(values, newValues, values.Length);
                newValues[values.Length] = value;
                Store[key] = newValues;
            }
            else
            {
                Set(key, value);
            }
        }

        public void AppendValues(string key, params string[] values)
        {
            ThrowIfSent();
            string[] oldValues;
            if (Store.TryGetValue(key, out oldValues))
            {
                var newValues = new string[oldValues.Length + values.Length];
                Array.Copy(oldValues, newValues, oldValues.Length);
                Array.Copy(values, 0, newValues, oldValues.Length, values.Length);
                Store[key] = newValues;
            }
            else
            {
                SetValues(key, values);
            }
        }

        public void Clear()
        {
            ThrowIfSent();
            Store.Clear();
        }

        public bool Contains(KeyValuePair<string, string[]> item)
        {
            return Store.Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return Store.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, string[]>[] array, int arrayIndex)
        {
            Store.CopyTo(array, arrayIndex);
        }

        public string Get(string key)
        {
            string[] values;
            if (Store.TryGetValue(key, out values))
            {
                return string.Join(", ", values);
            }
            return null;
        }

        public IEnumerator<KeyValuePair<string, string[]>> GetEnumerator()
        {
            return Store.GetEnumerator();
        }

        public IEnumerable<string> GetValues(string key)
        {
            string[] values;
            if (Store.TryGetValue(key, out values))
            {
                return HeaderParser.SplitValues(values);
            }
            return HeaderParser.Empty;
        }

        public bool Remove(KeyValuePair<string, string[]> item)
        {
            ThrowIfSent();
            return Store.Remove(item);
        }

        public bool Remove(string key)
        {
            ThrowIfSent();
            return Store.Remove(key);
        }

        public void Set(string key, string value)
        {
            ThrowIfSent();
            Store[key] = new[] { value };
        }

        public void SetValues(string key, params string[] values)
        {
            ThrowIfSent();
            Store[key] = values;
        }

        public bool TryGetValue(string key, out string[] value)
        {
            return Store.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void ThrowIfSent()
        {
            if (Sent)
            {
                throw new InvalidOperationException("The response headers cannot be modified because they have already been sent.");
            }
        }
    }
}