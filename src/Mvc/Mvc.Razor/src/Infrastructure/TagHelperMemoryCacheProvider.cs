// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.AspNetCore.Mvc.Razor.Infrastructure
{
    /// <summary>
    /// This API supports the MVC's infrastructure and is not intended to be used
    /// directly from your code. This API may change in future releases.
    /// </summary>
    public sealed class TagHelperMemoryCacheProvider
    {
        /// <summary>
        /// This API supports the MVC's infrastructure and is not intended to be used
        /// directly from your code. This API may change in future releases.
        /// </summary>
        public IMemoryCache Cache { get; internal set; } = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 10 * 1024 * 1024 // 10MB
        });
    }
}
