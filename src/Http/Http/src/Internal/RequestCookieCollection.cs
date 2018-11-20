// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.Internal
{
    public class RequestCookieCollection : IRequestCookieCollection
    {
        public static readonly RequestCookieCollection Empty = new RequestCookieCollection();
        private static readonly string[] EmptyKeys = Array.Empty<string>();
        private static readonly Enumerator EmptyEnumerator = new Enumerator();
        // Pre-box
        private static readonly IEnumerator<KeyValuePair<string, string>> EmptyIEnumeratorType = EmptyEnumerator;
        private static readonly IEnumerator EmptyIEnumerator = EmptyEnumerator;

        private Dictionary<string, string> Store { get; set; }

        public RequestCookieCollection()
        {
        }

        public RequestCookieCollection(Dictionary<string, string> store)
        {
            Store = store;
        }

        public RequestCookieCollection(int capacity)
        {
            Store = new Dictionary<string, string>(capacity, StringComparer.OrdinalIgnoreCase);
        }

        public string this[string key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                if (Store == null)
                {
                    return null;
                }

                string value;
                if (TryGetValue(key, out value))
                {
                    return value;
                }
                return null;
            }
        }

        public static RequestCookieCollection Parse(IList<string> values)
        {
            if (values.Count == 0)
            {
                return Empty;
            }

            IList<CookieHeaderValue> cookies;
            if (CookieHeaderValue.TryParseList(values, out cookies))
            {
                if (cookies.Count == 0)
                {
                    return Empty;
                }

                var collection = new RequestCookieCollection(cookies.Count);
                var store = collection.Store;
                for (var i = 0; i < cookies.Count; i++)
                {
                    var cookie = cookies[i];
                    var name = Uri.UnescapeDataString(cookie.Name.Value);
                    var value = Uri.UnescapeDataString(cookie.Value.Value);
                    store[name] = value;
                }

                return collection;
            }
            return Empty;
        }

        public int Count
        {
            get
            {
                if (Store == null)
                {
                    return 0;
                }
                return Store.Count;
            }
        }

        public ICollection<string> Keys
        {
            get
            {
                if (Store == null)
                {
                    return EmptyKeys;
                }
                return Store.Keys;
            }
        }

        public bool ContainsKey(string key)
        {
            if (Store == null)
            {
                return false;
            }
            return Store.ContainsKey(key);
        }

        public bool TryGetValue(string key, out string value)
        {
            if (Store == null)
            {
                value = null;
                return false;
            }
            return Store.TryGetValue(key, out value);
        }

        /// <summary>
        /// Returns an struct enumerator that iterates through a collection without boxing.
        /// </summary>
        /// <returns>An <see cref="Enumerator" /> object that can be used to iterate through the collection.</returns>
        public Enumerator GetEnumerator()
        {
            if (Store == null || Store.Count == 0)
            {
                // Non-boxed Enumerator
                return EmptyEnumerator;
            }
            // Non-boxed Enumerator
            return new Enumerator(Store.GetEnumerator());
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection, boxes in non-empty path.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}" /> object that can be used to iterate through the collection.</returns>
        IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
        {
            if (Store == null || Store.Count == 0)
            {
                // Non-boxed Enumerator
                return EmptyIEnumeratorType;
            }
            // Boxed Enumerator
            return GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection, boxes in non-empty path.
        /// </summary>
        /// <returns>An <see cref="IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            if (Store == null || Store.Count == 0)
            {
                // Non-boxed Enumerator
                return EmptyIEnumerator;
            }
            // Boxed Enumerator
            return GetEnumerator();
        }

        public struct Enumerator : IEnumerator<KeyValuePair<string, string>>
        {
            // Do NOT make this readonly, or MoveNext will not work
            private Dictionary<string, string>.Enumerator _dictionaryEnumerator;
            private bool _notEmpty;

            internal Enumerator(Dictionary<string, string>.Enumerator dictionaryEnumerator)
            {
                _dictionaryEnumerator = dictionaryEnumerator;
                _notEmpty = true;
            }

            public bool MoveNext()
            {
                if (_notEmpty)
                {
                    return _dictionaryEnumerator.MoveNext();
                }
                return false;
            }

            public KeyValuePair<string, string> Current
            {
                get
                {
                    if (_notEmpty)
                    {
                        var current = _dictionaryEnumerator.Current;
                        return new KeyValuePair<string, string>(current.Key, current.Value);
                    }
                    return default(KeyValuePair<string, string>);
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public void Dispose()
            {
            }

            public void Reset()
            {
                if (_notEmpty)
                {
                    ((IEnumerator)_dictionaryEnumerator).Reset();
                }
            }
        }
    }
}