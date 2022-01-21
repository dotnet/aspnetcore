// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.TagHelpers;

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
