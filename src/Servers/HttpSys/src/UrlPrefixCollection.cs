// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    /// <summary>
    /// A collection or URL prefixes
    /// </summary>
    public class UrlPrefixCollection : ICollection<UrlPrefix>
    {
        private readonly IDictionary<int, UrlPrefix> _prefixes = new Dictionary<int, UrlPrefix>(1);
        private UrlGroup _urlGroup;
        private int _nextId = 1;

        internal UrlPrefixCollection()
        {
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
                if (_urlGroup != null)
                {
                    _urlGroup.RegisterPrefix(item.FullPrefix, id);
                }
                _prefixes.Add(id, item);
            }
        }

        internal UrlPrefix GetPrefix(int id)
        {
            lock (_prefixes)
            {
                return _prefixes.TryGetValue(id, out var prefix) ? prefix : null;
            }
        }

        internal bool TryMatchLongestPrefix(bool isHttps, string host, string originalPath, out string pathBase, out string remainingPath)
        {
            var originalPathString = new PathString(originalPath);
            var found = false;
            pathBase = null;
            remainingPath = null;
            lock (_prefixes)
            {
                foreach (var prefix in _prefixes.Values)
                {
                    // The scheme, host, port, and start of path must match.
                    // Note this does not currently handle prefixes with wildcard subdomains.
                    if (isHttps == prefix.IsHttps
                        && string.Equals(host, prefix.HostAndPort, StringComparison.OrdinalIgnoreCase)
                        && originalPathString.StartsWithSegments(new PathString(prefix.PathWithoutTrailingSlash), StringComparison.OrdinalIgnoreCase, out var remainder)
                        && (!found || remainder.Value.Length < remainingPath.Length)) // Longest match
                    {
                        found = true;
                        pathBase = originalPath.Substring(0, prefix.PathWithoutTrailingSlash.Length); // Maintain the input casing
                        remainingPath = remainder.Value;
                    }
                }
            }
            return found;
        }

        public void Clear()
        {
            lock (_prefixes)
            {
                if (_urlGroup != null)
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
                        if (_urlGroup != null)
                        {
                            _urlGroup.UnregisterPrefix(pair.Value.FullPrefix);
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

        internal void RegisterAllPrefixes(UrlGroup urlGroup)
        {
            lock (_prefixes)
            {
                _urlGroup = urlGroup;
                // go through the uri list and register for each one of them
                foreach (var pair in _prefixes)
                {
                    // We'll get this index back on each request and use it to look up the prefix to calculate PathBase.
                    _urlGroup.RegisterPrefix(pair.Value.FullPrefix, pair.Key);
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
                    _urlGroup.UnregisterPrefix(prefix.FullPrefix);
                }
            }
        }
    }
}
