// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    /// <summary>
    /// A factory for <see cref="IMemoryCache"/>s configured using <see cref="CacheTagHelperOptions"/>.
    /// <see cref="CacheTagHelper"/> uses this factory to set its <see cref="CacheTagHelper.MemoryCache"/>.
    /// </summary>
    public class CacheTagHelperMemoryCacheFactory
    {
        /// <summary>
        /// Creates a new <see cref="CacheTagHelperMemoryCacheFactory"/>.
        /// </summary>
        /// <param name="options">The <see cref="CacheTagHelperOptions"/> to apply to the <see cref="Cache"/>.</param>
        public CacheTagHelperMemoryCacheFactory(IOptions<CacheTagHelperOptions> options)
        {
            Cache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = options.Value.SizeLimit
            });
        }

        // For testing only.
        internal CacheTagHelperMemoryCacheFactory(IMemoryCache cache)
        {
            Cache = cache;
        }

        /// <summary>
        /// Gets the <see cref="IMemoryCache"/>.
        /// </summary>
        public IMemoryCache Cache { get; }
    }
}
