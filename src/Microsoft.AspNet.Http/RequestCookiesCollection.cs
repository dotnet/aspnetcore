// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Framework.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Http.Internal
{
    public class RequestCookiesCollection : IReadableStringCollection
    {
        private readonly IDictionary<string, string> _dictionary;

        public RequestCookiesCollection()
        {
            _dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public StringValues this[string key]
        {
            get { return Get(key); }
        }

        /// <summary>
        /// Gets the number of elements contained in the collection.
        /// </summary>
        public int Count
        {
            get { return _dictionary.Count; }
        }

        /// <summary>
        /// Gets a collection containing the keys.
        /// </summary>
        public ICollection<string> Keys
        {
            get { return _dictionary.Keys; }
        }

        /// <summary>
        /// Determines whether the collection contains an element with the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(string key)
        {
            return _dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Get the associated value from the collection.  Multiple values will be merged.
        /// Returns null if the key is not present.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Get(string key)
        {
            string value;
            return _dictionary.TryGetValue(key, out value) ? value : null;
        }

        /// <summary>
        /// Get the associated values from the collection in their original format.
        /// Returns null if the key is not present.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public IList<string> GetValues(string key)
        {
            string value;
            return _dictionary.TryGetValue(key, out value) ? new[] { value } : null;
        }

        public void Reparse(IList<string> values)
        {
            _dictionary.Clear();

            IList<CookieHeaderValue> cookies;
            if (CookieHeaderValue.TryParseList(values, out cookies))
            {
                foreach (var cookie in cookies)
                {
                    var name = Uri.UnescapeDataString(cookie.Name.Replace('+', ' '));
                    var value = Uri.UnescapeDataString(cookie.Value.Replace('+', ' '));
                    _dictionary[name] = value;
                }
            }
        }

        public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
        {
            foreach (var pair in _dictionary)
            {
                yield return new KeyValuePair<string, StringValues>(pair.Key, pair.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}