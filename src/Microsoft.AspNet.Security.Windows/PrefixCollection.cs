// -----------------------------------------------------------------------
// <copyright file="HttpListenerPrefixCollection.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Security.Windows
{
    internal class PrefixCollection : ICollection<string>
    {
        private WindowsAuthMiddleware _winAuth;

        internal PrefixCollection(WindowsAuthMiddleware winAuth)
        {
            _winAuth = winAuth;
        }

        public int Count
        {
            get
            {
                return _winAuth._uriPrefixes.Count;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public void CopyTo(Array array, int offset)
        {
            if (Count > array.Length)
            {
                throw new ArgumentOutOfRangeException("array", SR.GetString(SR.net_array_too_small));
            }
            if (offset + Count > array.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            int index = 0;
            foreach (string uriPrefix in _winAuth._uriPrefixes.Keys)
            {
                array.SetValue(uriPrefix, offset + index++);
            }
        }

        public void CopyTo(string[] array, int offset)
        {
            if (Count > array.Length)
            {
                throw new ArgumentOutOfRangeException("array", SR.GetString(SR.net_array_too_small));
            }
            if (offset + Count > array.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            int index = 0;
            foreach (string uriPrefix in _winAuth._uriPrefixes.Keys)
            {
                array[offset + index++] = uriPrefix;
            }
        }

        public void Add(string uriPrefix)
        {
            _winAuth.AddPrefix(uriPrefix);
        }

        public bool Contains(string uriPrefix)
        {
            return _winAuth._uriPrefixes.Contains(uriPrefix);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerator<string> GetEnumerator()
        {
            return new PrefixEnumerator(_winAuth._uriPrefixes.Keys.GetEnumerator());
        }

        public bool Remove(string uriPrefix)
        {
            return _winAuth.RemovePrefix(uriPrefix);
        }

        public void Clear()
        {
            _winAuth.RemoveAll(true);
        }
    }
}
