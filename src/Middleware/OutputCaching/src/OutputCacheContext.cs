// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// Represent the current cache context for the request.
/// </summary>
public sealed class OutputCacheContext
{
    internal OutputCacheContext(HttpContext httpContext, IOutputCacheStore store, OutputCacheOptions options, ILogger logger)
    {
        HttpContext = httpContext;
        Logger = logger;
        Store = store;
        Options = options;
    }

    /// <summary>
    /// Determines whether the output caching logic should be configured for the incoming HTTP request.
    /// </summary>
    public bool EnableOutputCaching { get; set; }

    /// <summary>
    /// Determines whether a cache lookup is allowed for the incoming HTTP request.
    /// </summary>
    public bool AllowCacheLookup { get; set; }

    /// <summary>
    /// Determines whether storage of the response is allowed for the incoming HTTP request.
    /// </summary>
    public bool AllowCacheStorage { get; set; }

    /// <summary>
    /// Determines whether the request should be locked.
    /// </summary>
    public bool AllowLocking { get; set; }

    /// <summary>
    /// Gets the <see cref="HttpContext"/>.
    /// </summary>
    public HttpContext HttpContext { get; }

    /// <summary>
    /// Gets the response time.
    /// </summary>
    public DateTimeOffset? ResponseTime { get; internal set; }

    /// <summary>
    /// Gets the <see cref="CacheVaryByRules"/> instance.
    /// </summary>
    public CacheVaryByRules CacheVaryByRules { get; set; } = new();

    /// <summary>
    /// Gets the tags of the cached response.
    /// </summary>
    public HashSet<string> Tags { get; } = new();

    /// <summary>
    /// Gets or sets the amount of time the response should be cached for.
    /// </summary>
    public TimeSpan? ResponseExpirationTimeSpan { get; set; }

    internal string CacheKey { get; set; } = default!;

    internal TimeSpan CachedResponseValidFor { get; set; }

    internal bool IsCacheEntryFresh { get; set; }

    internal TimeSpan CachedEntryAge { get; set; }

    internal OutputCacheEntry CachedResponse { get; set; } = default!;

    internal bool ResponseStarted { get; set; }

    internal Stream OriginalResponseStream { get; set; } = default!;

    internal OutputCacheStream OutputCacheStream { get; set; } = default!;
    internal ILogger Logger { get; }
    internal OutputCacheOptions Options { get; }
    internal IOutputCacheStore Store { get; }
}
