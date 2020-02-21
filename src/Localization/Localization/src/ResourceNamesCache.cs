// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Extensions.Localization
{
    /// <summary>
    /// An implementation of <see cref="IResourceNamesCache"/> backed by a <see cref="ConcurrentDictionary{TKey, TValue}"/>.
    /// </summary>
    public class ResourceNamesCache : IResourceNamesCache
    {
        private readonly ConcurrentDictionary<string, IList<string>> _cache = new ConcurrentDictionary<string, IList<string>>();

        /// <summary>
        /// Creates a new <see cref="ResourceNamesCache" />
        /// </summary>
        public ResourceNamesCache()
        {
        }

        /// <inheritdoc />
        public IList<string> GetOrAdd(string name, Func<string, IList<string>> valueFactory)
        {
            return _cache.GetOrAdd(name, valueFactory);
        }
    }
}
