// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.AspNetCore.Mvc.TagHelpers.Cache
{
    /// <summary>
    /// An implementation of this interface provides a service to 
    /// cache distributed html fragments from the &lt;distributed-cache&gt;
    /// tag helper.
    /// </summary>
    public interface IDistributedCacheTagHelperStorage
    {
        /// <summary>
        /// Gets the content from the cache and deserializes it.
        /// </summary>
        /// <param name="key">The unique key to use in the cache.</param>
        /// <returns>The stored value if it exists, <value>null</value> otherwise.</returns>
        Task<byte[]> GetAsync(string key);

        /// <summary>
        /// Sets the content in the cache and serialized it.
        /// </summary>
        /// <param name="key">The unique key to use in the cache.</param>
        /// <param name="value">The value to cache.</param>
        /// <param name="options">The cache entry options.</param>
        Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options);
    }
}
