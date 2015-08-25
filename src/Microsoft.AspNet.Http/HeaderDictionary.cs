// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Primitives;

namespace Microsoft.AspNet.Http.Internal
{
    /// <summary>
    /// Represents a wrapper for owin.RequestHeaders and owin.ResponseHeaders.
    /// </summary>
    public class HeaderDictionary : IHeaderDictionary
    {
        public HeaderDictionary() : this(new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Microsoft.Owin.HeaderDictionary" /> class.
        /// </summary>
        /// <param name="store">The underlying data store.</param>
        public HeaderDictionary([NotNull] IDictionary<string, StringValues> store)
        {
            Store = store;
        }

        private IDictionary<string, StringValues> Store { get; set; }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.ICollection" /> that contains the keys in the <see cref="T:Microsoft.Owin.HeaderDictionary" />;.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.ICollection" /> that contains the keys in the <see cref="T:Microsoft.Owin.HeaderDictionary" />.</returns>
        public ICollection<string> Keys
        {
            get { return Store.Keys; }
        }

        /// <summary>
        /// 
        /// </summary>
        public ICollection<StringValues> Values
        {
            get { return Store.Values; }
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:Microsoft.Owin.HeaderDictionary" />;.
        /// </summary>
        /// <returns>The number of elements contained in the <see cref="T:Microsoft.Owin.HeaderDictionary" />.</returns>
        public int Count
        {
            get { return Store.Count; }
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="T:Microsoft.Owin.HeaderDictionary" /> is in read-only mode.
        /// </summary>
        /// <returns>true if the <see cref="T:Microsoft.Owin.HeaderDictionary" /> is in read-only mode; otherwise, false.</returns>
        public bool IsReadOnly
        {
            get { return Store.IsReadOnly; }
        }

        /// <summary>
        /// Get or sets the associated value from the collection as a single string.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <returns>the associated value from the collection as a StringValues or StringValues.Empty if the key is not present.</returns>
        public StringValues this[string key]
        {
            get { return ParsingHelpers.GetHeader(Store, key); }
            set { ParsingHelpers.SetHeader(Store, key, value); }
        }

        /// <summary>
        /// Throws KeyNotFoundException if the key is not present.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <returns></returns>
        StringValues IDictionary<string, StringValues>.this[string key]
        {
            get { return Store[key]; }
            set { Store[key] = value; }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator<KeyValuePair<string, StringValues>> IEnumerable<KeyValuePair<string, StringValues>>.GetEnumerator()
        {
            return Store.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Store.GetEnumerator();
        }

        /// <summary>
        /// Adds the given header and values to the collection.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <param name="value">The header values.</param>
        public void Add(string key, StringValues value)
        {
            Store.Add(key, value);
        }

        /// <summary>
        /// Determines whether the <see cref="T:Microsoft.Owin.HeaderDictionary" /> contains a specific key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>true if the <see cref="T:Microsoft.Owin.HeaderDictionary" /> contains a specific key; otherwise, false.</returns>
        public bool ContainsKey(string key)
        {
            return Store.ContainsKey(key);
        }

        /// <summary>
        /// Removes the given header from the collection.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <returns>true if the specified object was removed from the collection; otherwise, false.</returns>
        public bool Remove(string key)
        {
            return Store.Remove(key);
        }

        /// <summary>
        /// Retrieves a value from the dictionary.
        /// </summary>
        /// <param name="key">The header name.</param>
        /// <param name="value">The value.</param>
        /// <returns>true if the <see cref="T:Microsoft.Owin.HeaderDictionary" /> contains the key; otherwise, false.</returns>
        public bool TryGetValue(string key, out StringValues value)
        {
            return Store.TryGetValue(key, out value);
        }

        /// <summary>
        /// Adds a new list of items to the collection.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(KeyValuePair<string, StringValues> item)
        {
            Store.Add(item);
        }

        /// <summary>
        /// Clears the entire list of objects.
        /// </summary>
        public void Clear()
        {
            Store.Clear();
        }

        /// <summary>
        /// Returns a value indicating whether the specified object occurs within this collection.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>true if the specified object occurs within this collection; otherwise, false.</returns>
        public bool Contains(KeyValuePair<string, StringValues> item)
        {
            return Store.Contains(item);
        }

        /// <summary>
        /// Copies the <see cref="T:Microsoft.Owin.HeaderDictionary" /> elements to a one-dimensional Array instance at the specified index.
        /// </summary>
        /// <param name="array">The one-dimensional Array that is the destination of the specified objects copied from the <see cref="T:Microsoft.Owin.HeaderDictionary" />.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
        public void CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex)
        {
            Store.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removes the given item from the the collection.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>true if the specified object was removed from the collection; otherwise, false.</returns>
        public bool Remove(KeyValuePair<string, StringValues> item)
        {
            return Store.Remove(item);
        }
    }
}
