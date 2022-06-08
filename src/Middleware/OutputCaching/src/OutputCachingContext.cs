// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.OutputCaching;

internal sealed class OutputCachingContext : IOutputCachingContext
{
    internal OutputCachingContext(HttpContext httpContext, IOutputCacheStore store, ILogger logger)
    {
        HttpContext = httpContext;
        Logger = logger;
        Store = store;
    }

    /// <inheritdoc />
    public bool EnableOutputCaching { get; set; }

    /// <inheritdoc />
    public bool AllowCacheLookup { get; set; }

    /// <inheritdoc />
    public bool AllowCacheStorage { get; set; }

    /// <inheritdoc />
    public bool AllowLocking { get; set; }

    /// <inheritdoc />
    public HttpContext HttpContext { get; }

    /// <inheritdoc />
    public DateTimeOffset? ResponseTime { get; internal set; }

    /// <inheritdoc />
    public CachedVaryByRules CachedVaryByRules { get; set; } = new();

    /// <inheritdoc />
    public HashSet<string> Tags { get; } = new();

    /// <inheritdoc />
    public ILogger Logger { get; }

    /// <inheritdoc />
    public IOutputCacheStore Store { get; }

    /// <inheritdoc />
    public TimeSpan? ResponseExpirationTimeSpan { get; set; }

    internal string CacheKey { get; set; }

    internal TimeSpan CachedResponseValidFor { get; set; }

    internal bool IsCacheEntryFresh { get; set; }

    internal TimeSpan CachedEntryAge { get; set; }

    internal IOutputCacheEntry CachedResponse { get; set; }

    internal bool ResponseStarted { get; set; }

    internal Stream OriginalResponseStream { get; set; }

    internal OutputCachingStream OutputCachingStream { get; set; }
}
