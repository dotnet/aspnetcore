// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Owin
{
    internal class DictionaryStringValuesWrapper : IHeaderDictionary
    {
        public DictionaryStringValuesWrapper(IDictionary<string, string[]> inner)
        {
            Inner = inner;
        }

        public readonly IDictionary<string, string[]> Inner;

        private KeyValuePair<string, StringValues> Convert(KeyValuePair<string, string[]> item) => new KeyValuePair<string, StringValues>(item.Key, item.Value);

        private KeyValuePair<string, string[]> Convert(KeyValuePair<string, StringValues> item) => new KeyValuePair<string, string[]>(item.Key, item.Value);

        private StringValues Convert(string[] item) => item;

        private string[] Convert(StringValues item) => item;

        StringValues IHeaderDictionary.this[string key]
        {
            get
            {
                string[] values;
                return Inner.TryGetValue(key, out values) ? values : null;
            }
            set { Inner[key] = value; }
        }

        StringValues IDictionary<string, StringValues>.this[string key]
        {
            get { return Inner[key]; }
            set { Inner[key] = value; }
        }

        public long? ContentLength
        {
            get
            {
                long value;

                string[] rawValue;
                if (!Inner.TryGetValue(HeaderNames.ContentLength, out rawValue))
                {
                    return null;
                }

                if (rawValue.Length == 1 &&
                    !string.IsNullOrEmpty(rawValue[0]) &&
                    HeaderUtilities.TryParseNonNegativeInt64(new StringSegment(rawValue[0]).Trim(), out value))
                {
                    return value;
                }

                return null;
            }
            set
            {
                if (value.HasValue)
                {
                    Inner[HeaderNames.ContentLength] = (StringValues)HeaderUtilities.FormatNonNegativeInt64(value.Value);
                }
                else
                {
                    Inner.Remove(HeaderNames.ContentLength);
                }
            }
        }

        int ICollection<KeyValuePair<string, StringValues>>.Count => Inner.Count;

        bool ICollection<KeyValuePair<string, StringValues>>.IsReadOnly => Inner.IsReadOnly;

        ICollection<string> IDictionary<string, StringValues>.Keys => Inner.Keys;

        ICollection<StringValues> IDictionary<string, StringValues>.Values => Inner.Values.Select(Convert).ToList();

        void ICollection<KeyValuePair<string, StringValues>>.Add(KeyValuePair<string, StringValues> item) => Inner.Add(Convert(item));

        void IDictionary<string, StringValues>.Add(string key, StringValues value) => Inner.Add(key, value);

        void ICollection<KeyValuePair<string, StringValues>>.Clear() => Inner.Clear();

        bool ICollection<KeyValuePair<string, StringValues>>.Contains(KeyValuePair<string, StringValues> item) => Inner.Contains(Convert(item));

        bool IDictionary<string, StringValues>.ContainsKey(string key) => Inner.ContainsKey(key);

        void ICollection<KeyValuePair<string, StringValues>>.CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex)
        {
            foreach (var kv in Inner)
            {
                array[arrayIndex++] = Convert(kv);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => Inner.Select(Convert).GetEnumerator();

        IEnumerator<KeyValuePair<string, StringValues>> IEnumerable<KeyValuePair<string, StringValues>>.GetEnumerator() => Inner.Select(Convert).GetEnumerator();

        bool ICollection<KeyValuePair<string, StringValues>>.Remove(KeyValuePair<string, StringValues> item) => Inner.Remove(Convert(item));

        bool IDictionary<string, StringValues>.Remove(string key) => Inner.Remove(key);

        bool IDictionary<string, StringValues>.TryGetValue(string key, out StringValues value)
        {
            string[] temp;
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
