using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNet.Abstractions.Infrastructure;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.PipelineCore.Infrastructure;

namespace Microsoft.AspNet.PipelineCore.Collections
{
    /// <summary>
    /// Accessors for query, forms, etc.
    /// </summary>
    public class ReadableStringCollection : IReadableStringCollection
    {
        /// <summary>
        /// Create a new wrapper
        /// </summary>
        /// <param name="store"></param>
        public ReadableStringCollection(IDictionary<string, string[]> store)
        {
            if (store == null)
            {
                throw new ArgumentNullException("store");
            }

            Store = store;
        }

        private IDictionary<string, string[]> Store { get; set; }

        /// <summary>
        /// Get the associated value from the collection.  Multiple values will be merged.
        /// Returns null if the key is not present.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string this[string key]
        {
            get { return Get(key); }
        }

        /// <summary>
        /// Get the associated value from the collection.  Multiple values will be merged.
        /// Returns null if the key is not present.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Get(string key)
        {
            return ParsingHelpers.GetJoinedValue(Store, key);
        }

        /// <summary>
        /// Get the associated values from the collection in their original format.
        /// Returns null if the key is not present.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public IList<string> GetValues(string key)
        {
            string[] values;
            Store.TryGetValue(key, out values);
            return values;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<string, string[]>> GetEnumerator()
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
