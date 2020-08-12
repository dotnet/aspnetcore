// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.ResponseCaching
{
    internal interface IResponseCache
    {
        /// <summary>
        /// Gets the cached response for the given key, if it exists.
        /// If no cached response exists for the given key, <c>null</c> is returned.
        /// </summary>
        /// <param name="key">The cache key to look up.</param>
        /// <returns>The response cache entry if it exists; otherwise <c>null</c>.</returns>
        IResponseCacheEntry Get(string key);

        /// <summary>
        /// Stores the given response in the response cache.
        /// </summary>
        /// <param name="key">The cache key to store the response under.</param>
        /// <param name="entry">The response cache entry to store.</param>
        /// <param name="validFor">The amount of time the entry will be kept in the cache before expiring, relative to now.</param>
        void Set(string key, IResponseCacheEntry entry, TimeSpan validFor);
    }
}
