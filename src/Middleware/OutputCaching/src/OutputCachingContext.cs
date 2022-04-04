// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.OutputCaching;

public class OutputCachingContext : IOutputCachingContext
{
    private DateTimeOffset? _responseDate;
    private bool _parsedResponseDate;
    private DateTimeOffset? _responseExpires;
    private bool _parsedResponseExpires;
    private TimeSpan? _responseSharedMaxAge;
    private bool _parsedResponseSharedMaxAge;
    private TimeSpan? _responseMaxAge;
    private bool _parsedResponseMaxAge;

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
    public bool AttemptResponseCaching { get; set; }

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
    /// Determine whether the response retrieved from the response cache is fresh and can be served.
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

    internal OutputCacheEntry CachedResponse { get; set; }

    internal bool ResponseStarted { get; set; }

    internal Stream OriginalResponseStream { get; set; }

    internal OutputCachingStream OutputCachingStream { get; set; }

    public IHeaderDictionary CachedResponseHeaders { get; set; }

    public TimeSpan? ResponseExpirationTimeSpan { get; set; }

    public DateTimeOffset? ResponseDate
    {
        get
        {
            if (!_parsedResponseDate)
            {
                _parsedResponseDate = true;
                _responseDate = HeaderUtilities.TryParseDate(HttpContext.Response.Headers.Date.ToString(), out var date) ? date : null;
            }
            return _responseDate;
        }
        set
        {
            // Don't reparse the response date again if it's explicitly set
            _parsedResponseDate = true;
            _responseDate = value;
        }
    }

    public DateTimeOffset? ResponseExpires
    {
        get
        {
            if (!_parsedResponseExpires)
            {
                _parsedResponseExpires = true;
                _responseExpires = HeaderUtilities.TryParseDate(HttpContext.Response.Headers.Expires.ToString(), out var expires) ? expires : null;
            }
            return _responseExpires;
        }
    }

    public TimeSpan? ResponseSharedMaxAge
    {
        get
        {
            if (!_parsedResponseSharedMaxAge)
            {
                _parsedResponseSharedMaxAge = true;
                HeaderUtilities.TryParseSeconds(HttpContext.Response.Headers.CacheControl, CacheControlHeaderValue.SharedMaxAgeString, out _responseSharedMaxAge);
            }
            return _responseSharedMaxAge;
        }
    }

    public TimeSpan? ResponseMaxAge
    {
        get
        {
            if (!_parsedResponseMaxAge)
            {
                _parsedResponseMaxAge = true;
                HeaderUtilities.TryParseSeconds(HttpContext.Response.Headers.CacheControl, CacheControlHeaderValue.MaxAgeString, out _responseMaxAge);
            }
            return _responseMaxAge;
        }
    }
}
