// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Contains the parsed form values.
    /// </summary>
    public class FormCollection : IFormCollection
    {
        public static readonly FormCollection Empty = new FormCollection();
        private static readonly string[] EmptyKeys = Array.Empty<string>();
        private static readonly StringValues[] EmptyValues = Array.Empty<StringValues>();
        private static readonly Enumerator EmptyEnumerator = new Enumerator();
        // Pre-box
        private static readonly IEnumerator<KeyValuePair<string, StringValues>> EmptyIEnumeratorType = EmptyEnumerator;
        private static readonly IEnumerator EmptyIEnumerator = EmptyEnumerator;

        private static IFormFileCollection EmptyFiles = new FormFileCollection();

        private IFormFileCollection _files;

        private FormCollection()
        {
            // For static Empty
        }

        public FormCollection(Dictionary<string, StringValues> fields, IFormFileCollection files = null)
        {
            // can be null
            Store = fields;
            _files = files;
        }

        public IFormFileCollection Files
        {
            get
            {
                return _files ?? EmptyFiles;
            }
            private set { _files = value; }
        }

        private Dictionary<string, StringValues> Store { get; set; }

        /// <summary>
        /// Get or sets the associated value from the collection as a single string.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <returns>the associated value from the collection as a StringValues or StringValues.Empty if the key is not present.</returns>
        public StringValues this[string key]
        {
            get
            {
                if (Store == null)
                {
                    return StringValues.Empty;
                }

                StringValues value;
                if (TryGetValue(key, out value))
                {
                    return value;
                }
                return StringValues.Empty;
            }
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="HeaderDictionary" />;.
        /// </summary>
        /// <returns>The number of elements contained in the <see cref="HeaderDictionary" />.</returns>
        public int Count
        {
            get
            {
                return Store?.Count ?? 0;
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

        /// <summary>
        /// Determines whether the <see cref="HeaderDictionary" /> contains a specific key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>true if the <see cref="HeaderDictionary" /> contains a specific key; otherwise, false.</returns>
        public bool ContainsKey(string key)
        {
            if (Store == null)
            {
                return false;
            }
            return Store.ContainsKey(key);
        }

        /// <summary>
        /// Retrieves a value from the dictionary.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <param name="value">The value.</param>
        /// <returns>true if the <see cref="HeaderDictionary" /> contains the key; otherwise, false.</returns>
        public bool TryGetValue(string key, out StringValues value)
        {
            if (Store == null)
            {
                value = default(StringValues);
                return false;
            }
            return Store.TryGetValue(key, out value);
        }

        /// <summary>
        /// Returns an struct enumerator that iterates through a collection without boxing and is also used via the <see cref="IFormCollection" /> interface.
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
        /// <returns>An <see cref="IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator<KeyValuePair<string, StringValues>> IEnumerable<KeyValuePair<string, StringValues>>.GetEnumerator()
        {
            if (Store == null || Store.Count == 0)
            {
                // Non-boxed Enumerator
                return EmptyIEnumeratorType;
            }
            // Boxed Enumerator
            return Store.GetEnumerator();
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
            return Store.GetEnumerator();
        }

        public struct Enumerator : IEnumerator<KeyValuePair<string, StringValues>>
        {
            // Do NOT make this readonly, or MoveNext will not work
            private Dictionary<string, StringValues>.Enumerator _dictionaryEnumerator;
            private bool _notEmpty;

            internal Enumerator(Dictionary<string, StringValues>.Enumerator dictionaryEnumerator)
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

            public KeyValuePair<string, StringValues> Current
            {
                get
                {
                    if (_notEmpty)
                    {
                        return _dictionaryEnumerator.Current;
                    }
                    return default(KeyValuePair<string, StringValues>);
                }
            }

            public void Dispose()
            {
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            void IEnumerator.Reset()
            {
                if (_notEmpty)
                {
                    ((IEnumerator)_dictionaryEnumerator).Reset();
                }
            }
        }
    }
}
