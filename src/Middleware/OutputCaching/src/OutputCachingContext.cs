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

    /// <inheritdoc />
    public bool EnableOutputCaching { get; set; }

    /// <inheritdoc />
    public bool AttemptOutputCaching { get; set; }

    /// <inheritdoc />
    public bool AllowCacheLookup { get; set; }

    /// <inheritdoc />
    public bool AllowCacheStorage { get; set; }

    /// <inheritdoc />
    public bool AllowLocking { get; set; }

    /// <inheritdoc />
    public bool IsResponseCacheable { get; set; }

    /// <inheritdoc />
    public bool IsCacheEntryFresh { get; set; }

    /// <inheritdoc />
    public HttpContext HttpContext { get; }

    /// <inheritdoc />
    public DateTimeOffset? ResponseTime { get; internal set; }

    /// <inheritdoc />
    public TimeSpan? CachedEntryAge { get; internal set; }

    /// <inheritdoc />
    public CachedVaryByRules CachedVaryByRules { get; set; } = new();

    /// <inheritdoc />
    public HashSet<string> Tags { get; } = new();

    /// <inheritdoc />
    public ILogger Logger { get; }

    /// <inheritdoc />
    public TimeSpan? ResponseExpirationTimeSpan { get; set; }

    internal string CacheKey { get; set; }

    /// <summary>
    /// Gets or sets the amount of time the response is cached for.
    /// </summary>
    /// <remarks>
    /// It is computed from either <see cref="ResponseExpirationTimeSpan" /> or <see cref="OutputCachingOptions.DefaultExpirationTimeSpan"/>.
    /// </remarks>
    internal TimeSpan CachedResponseValidFor { get; set; }

    internal IOutputCacheEntry CachedResponse { get; set; }

    internal bool ResponseStarted { get; set; }

    internal Stream OriginalResponseStream { get; set; }

    internal OutputCachingStream OutputCachingStream { get; set; }
}
