// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.OutputCaching;

internal class OutputCachingContext : IOutputCachingContext
{
    internal OutputCachingContext(HttpContext httpContext, ILogger logger)
    {
        HttpContext = httpContext;
        Logger = logger;
    }

    /// <summary>
    /// Determine whether the output caching logic should is configured for the incoming HTTP request.
    /// </summary>
    public bool EnableOutputCaching { get; set; }

    /// <summary>
    /// Determine whether the response caching logic should be attempted for the incoming HTTP request.
    /// </summary>
    public bool AttemptOutputCaching { get; set; }

    /// <summary>
    /// Determine whether a cache lookup is allowed for the incoming HTTP request.
    /// </summary>
    public bool AllowCacheLookup { get; set; }

    /// <summary>
    /// Determine whether storage of the response is allowed for the incoming HTTP request.
    /// </summary>
    public bool AllowCacheStorage { get; set; }

    /// <summary>
    /// Determine whether request should be locked.
    /// </summary>
    public bool AllowLocking { get; set; }

    /// <summary>
    /// Determine whether the response received by the middleware can be cached for future requests.
    /// </summary>
    public bool IsResponseCacheable { get; set; }

    /// <summary>
    /// Determine whether the response retrieved from the cache store is fresh and can be served.
    /// </summary>
    public bool IsCacheEntryFresh { get; set; }

    public HttpContext HttpContext { get; }

    public DateTimeOffset? ResponseTime { get; internal set; }

    public TimeSpan? CachedEntryAge { get; internal set; }

    public CachedVaryByRules CachedVaryByRules { get; set; } = new();

    public HashSet<string> Tags { get; } = new();

    public ILogger Logger { get; }

    internal string CacheKey { get; set; }

    internal TimeSpan CachedResponseValidFor { get; set; }

    internal IOutputCacheEntry CachedResponse { get; set; }

    internal bool ResponseStarted { get; set; }

    internal Stream OriginalResponseStream { get; set; }

    internal OutputCachingStream OutputCachingStream { get; set; }

    public IHeaderDictionary CachedResponseHeaders { get; set; }

    public TimeSpan? ResponseExpirationTimeSpan { get; set; }
}
