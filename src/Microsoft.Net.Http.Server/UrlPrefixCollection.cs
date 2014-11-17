// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Net.Http.Server
{
    /// <summary>
    /// A collection or URL prefixes
    /// </summary>
    public class UrlPrefixCollection : ICollection<UrlPrefix>
    {
        private readonly WebListener _webListener;
        private readonly IDictionary<int, UrlPrefix> _prefixes = new Dictionary<int, UrlPrefix>(1);
        private int _nextId = 1;

        internal UrlPrefixCollection(WebListener webListener)
        {
            _webListener = webListener;
        }

        public int Count
        {
            get
            {
                lock (_prefixes)
                {
                    return _prefixes.Count;
                }
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public void Add(string prefix)
        {
            Add(UrlPrefix.Create(prefix));
        }

        public void Add(UrlPrefix item)
        {
            lock (_prefixes)
            {
                var id = _nextId++;
                if (_webListener.IsListening)
                {
                    RegisterPrefix(item.Whole, id);
                }
                _prefixes.Add(id, item);
            }
        }

        internal UrlPrefix GetPrefix(int id)
        {
            lock (_prefixes)
            {
                return _prefixes[id];
            }
        }

        public void Clear()
        {
            lock (_prefixes)
            {
                if (_webListener.IsListening)
                {
                    UnregisterAllPrefixes();
                }
                _prefixes.Clear();
            }
        }

        public bool Contains(UrlPrefix item)
        {
            lock (_prefixes)
            {
                return _prefixes.Values.Contains(item);
            }
        }

        public void CopyTo(UrlPrefix[] array, int arrayIndex)
        {
            lock (_prefixes)
            {
                _prefixes.Values.CopyTo(array, arrayIndex);
            }
        }

        public bool Remove(string prefix)
        {
            return Remove(UrlPrefix.Create(prefix));
        }

        public bool Remove(UrlPrefix item)
        {
            lock (_prefixes)
            {
                int? id = null;
                foreach (var pair in _prefixes)
                {
                    if (pair.Value.Equals(item))
                    {
                        id = pair.Key;
                        if (_webListener.IsListening)
                        {
                            UnregisterPrefix(pair.Value.Whole);
                        }
                    }
                }
                if (id.HasValue)
                {
                    _prefixes.Remove(id.Value);
                    return true;
                }
                return false;
            }
        }

        public IEnumerator<UrlPrefix> GetEnumerator()
        {
            lock (_prefixes)
            {
                return _prefixes.Values.GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal void RegisterAllPrefixes()
        {
            lock (_prefixes)
            {
                // go through the uri list and register for each one of them
                foreach (var pair in _prefixes)
                {
                    // We'll get this index back on each request and use it to look up the prefix to calculate PathBase.
                    RegisterPrefix(pair.Value.Whole, pair.Key);
                }
            }
        }

        internal void UnregisterAllPrefixes()
        {
            lock (_prefixes)
            {
                // go through the uri list and unregister for each one of them
                foreach (var prefix in _prefixes.Values)
                {
                    // ignore possible failures
                    UnregisterPrefix(prefix.Whole);
                }
            }
        }

        private void RegisterPrefix(string uriPrefix, int contextId)
        {
            LogHelper.LogInfo(_webListener.Logger, "Listening on prefix: " + uriPrefix);

            uint statusCode =
                UnsafeNclNativeMethods.HttpApi.HttpAddUrlToUrlGroup(
                    _webListener.UrlGroupId,
                    uriPrefix,
                    (ulong)contextId,
                    0);

            if (statusCode != UnsafeNclNativeMethods.ErrorCodes.ERROR_SUCCESS)
            {
                if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_ALREADY_EXISTS)
                {
                    throw new WebListenerException((int)statusCode, String.Format(Resources.Exception_PrefixAlreadyRegistered, uriPrefix));
                }
                else
                {
                    throw new WebListenerException((int)statusCode);
                }
            }
        }

        private bool UnregisterPrefix(string uriPrefix)
        {
            uint statusCode = 0;
            LogHelper.LogInfo(_webListener.Logger, "Stop listening on prefix: " + uriPrefix);

            statusCode =
                UnsafeNclNativeMethods.HttpApi.HttpRemoveUrlFromUrlGroup(
                    _webListener.UrlGroupId,
                    uriPrefix,
                    0);

            if (statusCode == UnsafeNclNativeMethods.ErrorCodes.ERROR_NOT_FOUND)
            {
                return false;
            }
            return true;
        }
    }
}