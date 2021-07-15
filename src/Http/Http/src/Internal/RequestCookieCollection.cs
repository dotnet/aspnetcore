// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Internal;
using Microsoft.Net.Http.Headers;
using System.Linq;

namespace Microsoft.AspNetCore.Http
{
    internal class RequestCookieCollection : IRequestCookieCollection
    {
        public static readonly RequestCookieCollection Empty = new RequestCookieCollection();
        private static readonly string[] EmptyKeys = Array.Empty<string>();

        // Pre-box
        private static readonly IEnumerator<KeyValuePair<string, string>> EmptyIEnumeratorType = default(Enumerator);
        private static readonly IEnumerator EmptyIEnumerator = default(Enumerator);

        private AdaptiveCapacityDictionary<string, string> Store { get; set; }

        public RequestCookieCollection()
        {
            Store = new AdaptiveCapacityDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public RequestCookieCollection(int capacity)
        {
            Store = new AdaptiveCapacityDictionary<string, string>(capacity, StringComparer.OrdinalIgnoreCase);
        }

        // For tests
        public RequestCookieCollection(Dictionary<string, string> store)
        {
            Store = new AdaptiveCapacityDictionary<string, string>(store);
        }

        public string? this[string key]
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

                if (TryGetValue(key, out var value))
                {
                    return value;
                }
                return null;
            }
        }

        public static RequestCookieCollection Parse(StringValues values)
           => ParseInternal(values, AppContext.TryGetSwitch(ResponseCookies.EnableCookieNameEncoding, out var enabled) && enabled);

        internal static RequestCookieCollection ParseInternal(StringValues values, bool enableCookieNameEncoding)
        {
            if (values.Count == 0)
            {
                return Empty;
            }

            // Do not set the collection capacity based on StringValues.Count, the Cookie header is supposed to be a single combined value.
            var collection = new RequestCookieCollection();
            var store = collection.Store!;

            if (CookieHeaderParserShared.TryParseValues(values, store, enableCookieNameEncoding, supportsMultipleValues: true))
            {
                if (store.Count == 0)
                {
                    return Empty;
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

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out string? value)
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
                return default;
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
            private AdaptiveCapacityDictionary<string, string>.Enumerator _dictionaryEnumerator;
            private readonly bool _notEmpty;

            internal Enumerator(AdaptiveCapacityDictionary<string, string>.Enumerator dictionaryEnumerator)
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
                        return new KeyValuePair<string, string>(current.Key, (string)current.Value!);
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
