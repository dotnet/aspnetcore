// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// Represents a cached response entry.
/// </summary>
public class OutputCacheEntry
{
    /// <summary>
    /// Gets the created date and time of the cache entry.
    /// </summary>
    public DateTimeOffset Created { get; set; }

    /// <summary>
    /// Gets the status code of the cache entry.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets the headers of the cache entry.
    /// </summary>
    public IHeaderDictionary Headers { get; set; } = default!;

    /// <summary>
    /// Gets the body of the cache entry.
    /// </summary>
    public CachedResponseBody Body { get; set; } = default!;

    /// <summary>
    /// Gets the tags of the cache entry.
    /// </summary>
    public string[]? Tags { get; set; }
}
