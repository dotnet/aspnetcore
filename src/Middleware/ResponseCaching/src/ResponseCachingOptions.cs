// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.ResponseCaching;

/// <summary>
/// Options for configuring the <see cref="ResponseCachingMiddleware"/>.
/// </summary>
public class ResponseCachingOptions
{
    /// <summary>
    /// The size limit for the response cache middleware in bytes. The default is set to 100 MB.
    /// When this limit is exceeded, no new responses will be cached until older entries are
    /// evicted.
    /// </summary>
    public long SizeLimit { get; set; } = 100 * 1024 * 1024;

    /// <summary>
    /// The largest cacheable size for the response body in bytes. The default is set to 64 MB.
    /// If the response body exceeds this limit, it will not be cached by the <see cref="ResponseCachingMiddleware"/>.
    /// </summary>
    public long MaximumBodySize { get; set; } = 64 * 1024 * 1024;

    /// <summary>
    /// <c>true</c> if request paths are case-sensitive; otherwise <c>false</c>. The default is to treat paths as case-insensitive.
    /// </summary>
    public bool UseCaseSensitivePaths { get; set; }

    /// <summary>
    /// For testing purposes only.
    /// </summary>
    internal TimeProvider TimeProvider { get; set; } = TimeProvider.System;
}
