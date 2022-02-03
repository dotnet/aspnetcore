// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.AspNetCore.Mvc.Razor.Infrastructure;

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
