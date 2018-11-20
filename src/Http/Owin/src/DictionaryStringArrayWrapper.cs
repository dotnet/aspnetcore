// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Owin
{
    internal class DictionaryStringArrayWrapper : IDictionary<string, string[]>
    {
        public DictionaryStringArrayWrapper(IHeaderDictionary inner)
        {
            Inner = inner;
        }

        public readonly IHeaderDictionary Inner;

        private KeyValuePair<string, StringValues> Convert(KeyValuePair<string, string[]> item) => new KeyValuePair<string, StringValues>(item.Key, item.Value);

        private KeyValuePair<string, string[]> Convert(KeyValuePair<string, StringValues> item) => new KeyValuePair<string, string[]>(item.Key, item.Value);

        private StringValues Convert(string[] item) => item;

        private string[] Convert(StringValues item) => item;

        string[] IDictionary<string, string[]>.this[string key]
        {
            get { return ((IDictionary<string, StringValues>)Inner)[key]; }
            set { Inner[key] = value; }
        }

        int ICollection<KeyValuePair<string, string[]>>.Count => Inner.Count;

        bool ICollection<KeyValuePair<string, string[]>>.IsReadOnly => Inner.IsReadOnly;

        ICollection<string> IDictionary<string, string[]>.Keys => Inner.Keys;

        ICollection<string[]> IDictionary<string, string[]>.Values => Inner.Values.Select(Convert).ToList();

        void ICollection<KeyValuePair<string, string[]>>.Add(KeyValuePair<string, string[]> item) => Inner.Add(Convert(item));

        void IDictionary<string, string[]>.Add(string key, string[] value) => Inner.Add(key, value);

        void ICollection<KeyValuePair<string, string[]>>.Clear() => Inner.Clear();

        bool ICollection<KeyValuePair<string, string[]>>.Contains(KeyValuePair<string, string[]> item) => Inner.Contains(Convert(item));

        bool IDictionary<string, string[]>.ContainsKey(string key) => Inner.ContainsKey(key);

        void ICollection<KeyValuePair<string, string[]>>.CopyTo(KeyValuePair<string, string[]>[] array, int arrayIndex)
        {
            foreach(var kv in Inner)
            {
                array[arrayIndex++] = Convert(kv);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => Inner.Select(Convert).GetEnumerator();

        IEnumerator<KeyValuePair<string, string[]>> IEnumerable<KeyValuePair<string, string[]>>.GetEnumerator() => Inner.Select(Convert).GetEnumerator();

        bool ICollection<KeyValuePair<string, string[]>>.Remove(KeyValuePair<string, string[]> item) => Inner.Remove(Convert(item));

        bool IDictionary<string, string[]>.Remove(string key) => Inner.Remove(key);

        bool IDictionary<string, string[]>.TryGetValue(string key, out string[] value)
        {
            StringValues temp;
            if (Inner.TryGetValue(key, out temp))
            {
                value = temp;
                return true;
            }
            value = default(StringValues);
            return false;
        }
    }
}
