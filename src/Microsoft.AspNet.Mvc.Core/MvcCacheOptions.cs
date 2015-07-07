// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Provides programmatic configuration for caching in the MVC framework.
    /// </summary>
    public class MvcCacheOptions
    {
        /// <summary>
        /// Gets a Dictionary of CacheProfile Names, <see cref="CacheProfile"/> which are pre-defined settings for
        /// <see cref="ResponseCacheFilter"/>.
        /// </summary>
        public IDictionary<string, CacheProfile> CacheProfiles { get; } = 
            new Dictionary<string, CacheProfile>(StringComparer.OrdinalIgnoreCase);
    }
}