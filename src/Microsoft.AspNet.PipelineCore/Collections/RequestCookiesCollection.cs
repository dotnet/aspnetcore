// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Core.Infrastructure;

namespace Microsoft.AspNet.Http.Core.Collections
{
    public class RequestCookiesCollection : IReadableStringCollection
    {
        private readonly IDictionary<string, string> _dictionary;

        public RequestCookiesCollection()
        {
            _dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public string this[string key]
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

        private static readonly char[] SemicolonAndComma = { ';', ',' };

        public void Reparse(string cookiesHeader)
        {
            _dictionary.Clear();
            ParsingHelpers.ParseDelimited(cookiesHeader, SemicolonAndComma, AddCookieCallback, _dictionary);
        }

        public IEnumerator<KeyValuePair<string, string[]>> GetEnumerator()
        {
            foreach (var pair in _dictionary)
            {
                yield return new KeyValuePair<string, string[]>(pair.Key, new[] { pair.Value });
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private static readonly Action<string, string, object> AddCookieCallback = (name, value, state) =>
        {
            var dictionary = (IDictionary<string, string>)state;
            if (!dictionary.ContainsKey(name))
            {
                dictionary.Add(name, value);
            }
        };
    }
}