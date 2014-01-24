using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.PipelineCore.Collections
{
    /// <summary>
    /// A wrapper for the request Cookie header
    /// </summary>
    public class RequestCookieCollection : IEnumerable<KeyValuePair<string, string>>
    {
        /// <summary>
        /// Create a new wrapper
        /// </summary>
        /// <param name="store"></param>
        public RequestCookieCollection(IDictionary<string, string> store)
        {
            if (store == null)
            {
                throw new ArgumentNullException("store");
            }

            Store = store;
        }

        private IDictionary<string, string> Store { get; set; }

        /// <summary>
        /// Returns null rather than throwing KeyNotFoundException
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string this[string key]
        {
            get
            {
                string value;
                Store.TryGetValue(key, out value);
                return value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return Store.GetEnumerator();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
