// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    /// <summary>
    /// Provides programmatic configuration for the cache tag helper in the MVC framework.
    /// </summary>
    public class CacheTagHelperOptions
    {
        /// <summary>
        /// The maximum total size in bytes that will be cached by the <see cref="CacheTagHelper"/>
        /// at any given time.
        /// </summary>
        public long SizeLimit { get; set; } = 100 * 1024 * 1024; // 100MB
    }
}
