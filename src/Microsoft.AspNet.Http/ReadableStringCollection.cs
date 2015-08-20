// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Primitives;

namespace Microsoft.AspNet.Http.Internal
{
    /// <summary>
    /// Accessors for query, forms, etc.
    /// </summary>
    public class ReadableStringCollection : IReadableStringCollection
    {
        public static readonly IReadableStringCollection Empty = new ReadableStringCollection(new Dictionary<string, StringValues>(0));

        /// <summary>
        /// Create a new wrapper
        /// </summary>
        /// <param name="store"></param>
        public ReadableStringCollection([NotNull] IDictionary<string, StringValues> store)
        {
            Store = store;
        }

        private IDictionary<string, StringValues> Store { get; set; }

        /// <summary>
        /// Gets the number of elements contained in the collection.
        /// </summary>
        public int Count
        {
            get { return Store.Count; }
        }

        /// <summary>
        /// Gets a collection containing the keys.
        /// </summary>
        public ICollection<string> Keys
        {
            get { return Store.Keys; }
        }


        /// <summary>
        /// Get the associated value from the collection.  Multiple values will be merged.
        /// Returns StringValues.Empty if the key is not present.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public StringValues this[string key]
        {
            get
            {
                StringValues value;
                if (Store.TryGetValue(key, out value))
                {
                    return value;
                }
                return StringValues.Empty;
            }
        }

        /// <summary>
        /// Determines whether the collection contains an element with the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(string key)
        {
            return Store.ContainsKey(key);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
        {
            return Store.GetEnumerator();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
