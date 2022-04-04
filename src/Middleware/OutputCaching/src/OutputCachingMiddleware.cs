// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// Enable HTTP response caching.
/// </summary>
public class OutputCachingMiddleware
{
    // see https://tools.ietf.org/html/rfc7232#section-4.1
    private static readonly string[] HeadersToIncludeIn304 =
        new[] { "Cache-Control", "Content-Location", "Date", "ETag", "Expires", "Vary" };

    private readonly RequestDelegate _next;
    private readonly OutputCachingOptions _options;
    private readonly ILogger _logger;
    private readonly IOutputCachingPolicyProvider _policyProvider;
    private readonly IOutputCacheStore _cache;
    private readonly IOutputCachingKeyProvider _keyProvider;
    private readonly WorkDispatcher<string, OutputCacheEntry?> _outputCacheEntryDispatcher;
    private readonly WorkDispatcher<string, OutputCacheEntry?> _requestDispatcher;

    /// <summary>
    /// Creates a new <see cref="OutputCachingMiddleware"/>.
    /// </summary>
    /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
    /// <param name="options">The options for this middleware.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> used for logging.</param>
    /// <param name="poolProvider">The <see cref="ObjectPoolProvider"/> used for creating <see cref="ObjectPool"/> instances.</param>
    public OutputCachingMiddleware(
        RequestDelegate next,
        IOptions<OutputCachingOptions> options,
        ILoggerFactory loggerFactory,
        IOutputCacheStore outputCache,
        ObjectPoolProvider poolProvider
        )
        : this(
            next,
            options,
            loggerFactory,
            new OutputCachingPolicyProvider(options),
            outputCache,
            new OutputCachingKeyProvider(poolProvider, options))
    { }

    // for testing
    internal OutputCachingMiddleware(
        RequestDelegate next,
        IOptions<OutputCachingOptions> options,
        ILoggerFactory loggerFactory,
        IOutputCachingPolicyProvider policyProvider,
        IOutputCacheStore cache,
        IOutputCachingKeyProvider keyProvider)
    {
        ArgumentNullException.ThrowIfNull(next, nameof(next));
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ArgumentNullException.ThrowIfNull(loggerFactory, nameof(loggerFactory));
        ArgumentNullException.ThrowIfNull(policyProvider, nameof(policyProvider));
        ArgumentNullException.ThrowIfNull(cache, nameof(cache));
        ArgumentNullException.ThrowIfNull(keyProvider, nameof(keyProvider));

        _next = next;
        _options = options.Value;
        _logger = loggerFactory.CreateLogger<OutputCachingMiddleware>();
        _policyProvider = policyProvider;
        _cache = cache;
        _keyProvider = keyProvider;
        _outputCacheEntryDispatcher = new();
        _requestDispatcher = new();
    }

    /// <summary>
    /// Invokes the logic of the middleware.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    /// <returns>A <see cref="Task"/> that completes when the middleware has completed processing.</returns>
    public async Task Invoke(HttpContext httpContext)
    {
        var context = new OutputCachingContext(httpContext, _logger);

        // Add IOutputCachingFeature
        AddOutputCachingFeature(context);

        try
        {
            await _policyProvider.OnRequestAsync(context);

            // Should we attempt any caching logic?
            if (context.EnableOutputCaching && context.AttemptResponseCaching)
            {
                // Can this request be served from cache?
                if (context.AllowCacheLookup)
                {
                    CreateCacheKey(context);

                    // Locking cache lookups by default
                    // TODO: should it be part of the cache implementations or can we assume all caches would benefit from it?
                    // It makes sense for caches that use IO (disk, network) or need to deserialize the state but could also be a global option

                    var cacheEntry = await _outputCacheEntryDispatcher.ScheduleAsync(context.CacheKey, _cache, static async (key, cache) => await cache.GetAsync(key));

                    if (await TryServeFromCacheAsync(context, cacheEntry))
                    {
                        return;
                    }
                }

                // Should we store the response to this request?
                if (context.AllowCacheStorage)
                {
                    // It is also a pre-condition to reponse locking

                    while (true)
                    {
                        var executed = false;

                        if (context.AllowLocking)
                        {
                            var cacheEntry = await _requestDispatcher.ScheduleAsync(context.CacheKey, key => ExecuteResponseAsync());

                            // If the result was processed by another request, serve it from cache
                            if (!executed)
                            {
                                if (await TryServeFromCacheAsync(context, cacheEntry))
                                {
                                    return;
                                }
                            }
                        }
                        else
                        {
                            await ExecuteResponseAsync();
                        }

                        async Task<OutputCacheEntry?> ExecuteResponseAsync()
                        {
                            // Hook up to listen to the response stream
                            ShimResponseStream(context);

                            try
                            {
                                await _next(httpContext);

                                // The next middleware might change the policy
                                await _policyProvider.OnServeResponseAsync(context);

                                // If there was no response body, check the response headers now. We can cache things like redirects.
                                StartResponse(context);

                                // Finalize the cache entry
                                await FinalizeCacheBodyAsync(context);

                                executed = true;
                            }
                            finally
                            {
                                UnshimResponseStream(context);
                            }

                            return context.CachedResponse;
                        }

                        return;
                    }
                }
            }

            await _next(httpContext);
        }
        finally
        {
            RemoveOutputCachingFeature(httpContext);
        }
    }

    internal async Task<bool> TryServeCachedResponseAsync(OutputCachingContext context, OutputCacheEntry cachedResponse)
    {
        context.CachedResponse = cachedResponse;
        context.CachedResponseHeaders = cachedResponse.Headers;
        context.ResponseTime = _options.SystemClock.UtcNow;
        var cachedEntryAge = context.ResponseTime.Value - context.CachedResponse.Created;
        context.CachedEntryAge = cachedEntryAge > TimeSpan.Zero ? cachedEntryAge : TimeSpan.Zero;

        await _policyProvider.OnServeFromCacheAsync(context);

        if (context.IsCacheEntryFresh)
        {
            // Check conditional request rules
            if (ContentIsNotModified(context))
            {
                _logger.NotModifiedServed();
                context.HttpContext.Response.StatusCode = StatusCodes.Status304NotModified;

                if (context.CachedResponseHeaders != null)
                {
                    foreach (var key in HeadersToIncludeIn304)
                    {
                        if (context.CachedResponseHeaders.TryGetValue(key, out var values))
                        {
                            context.HttpContext.Response.Headers[key] = values;
                        }
                    }
                }
            }
            else
            {
                var response = context.HttpContext.Response;
                // Copy the cached status code and response headers
                response.StatusCode = context.CachedResponse.StatusCode;
                foreach (var header in context.CachedResponse.Headers)
                {
                    response.Headers[header.Key] = header.Value;
                }

                // Note: int64 division truncates result and errors may be up to 1 second. This reduction in
                // accuracy of age calculation is considered appropriate since it is small compared to clock
                // skews and the "Age" header is an estimate of the real age of cached content.
                response.Headers.Age = HeaderUtilities.FormatNonNegativeInt64(context.CachedEntryAge.Value.Ticks / TimeSpan.TicksPerSecond);

                // Copy the cached response body
                var body = context.CachedResponse.Body;
                if (body.Length > 0)
                {
                    try
                    {
                        await body.CopyToAsync(response.BodyWriter, context.HttpContext.RequestAborted);
                    }
                    catch (OperationCanceledException)
                    {
                        context.HttpContext.Abort();
                    }
                }
                _logger.CachedResponseServed();
            }
            return true;
        }

        return false;
    }

    internal async Task<bool> TryServeFromCacheAsync(OutputCachingContext context, OutputCacheEntry? cacheEntry)
    {
        if (cacheEntry != null)
        {
            if (await TryServeCachedResponseAsync(context, cacheEntry))
            {
                return true;
            }
        }

        if (HeaderUtilities.ContainsCacheDirective(context.HttpContext.Request.Headers.CacheControl, CacheControlHeaderValue.OnlyIfCachedString))
        {
            _logger.GatewayTimeoutServed();
            context.HttpContext.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
            return true;
        }

        _logger.NoResponseServed();
        return false;
    }

    private void CreateCacheKey(OutputCachingContext context)
    {
        var varyHeaders = new StringValues(context.HttpContext.Response.Headers.GetCommaSeparatedValues(HeaderNames.Vary));
        var varyQueryKeys = context.CachedVaryByRules.QueryKeys;
        var varyByCustomKeys = context.CachedVaryByRules.VaryByCustom;
        var varyByPrefix = context.CachedVaryByRules.VaryByPrefix;

        // Check if any vary rules exist
        if (!StringValues.IsNullOrEmpty(varyHeaders) || !StringValues.IsNullOrEmpty(varyQueryKeys) || !StringValues.IsNullOrEmpty(varyByPrefix) || varyByCustomKeys.Count > 0)
        {
            // Normalize order and casing of vary by rules
            var normalizedVaryHeaders = GetOrderCasingNormalizedStringValues(varyHeaders);
            var normalizedVaryQueryKeys = GetOrderCasingNormalizedStringValues(varyQueryKeys);
            var normalizedVaryByCustom = GetOrderCasingNormalizedDictionary(varyByCustomKeys);

            // Update vary rules with normalized values
            context.CachedVaryByRules = new CachedVaryByRules
            {
                VaryByPrefix = varyByPrefix + normalizedVaryByCustom,
                Headers = normalizedVaryHeaders,
                QueryKeys = normalizedVaryQueryKeys
            };

            // TODO: Add same condition on LogLevel in Response Caching
            // Always overwrite the CachedVaryByRules to update the expiry information
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.VaryByRulesUpdated(normalizedVaryHeaders.ToString(), normalizedVaryQueryKeys.ToString());
            }
        }

        context.CacheKey = _keyProvider.CreateStorageVaryByKey(context);
    }

    /// <summary>
    /// Finalize cache headers.
    /// </summary>
    /// <param name="context"></param>
    private void FinalizeCacheHeaders(OutputCachingContext context)
    {
        if (context.IsResponseCacheable)
        {
            // Create the cache entry now
            var response = context.HttpContext.Response;
            var headers = response.Headers;

            context.CachedResponseValidFor = context.ResponseSharedMaxAge ??
                context.ResponseMaxAge ??
                (context.ResponseExpires - context.ResponseTime!.Value) ??
                context.ResponseExpirationTimeSpan ?? _options.DefaultExpirationTimeSpan;

            // Ensure date header is set
            if (!context.ResponseDate.HasValue)
            {
                context.ResponseDate = context.ResponseTime!.Value;
                // Setting the date on the raw response headers.
                headers.Date = HeaderUtilities.FormatDate(context.ResponseDate.Value);
            }

            // Store the response on the state
            context.CachedResponse = new OutputCacheEntry
            {
                Created = context.ResponseDate.Value,
                StatusCode = response.StatusCode,
                Headers = new HeaderDictionary(),
                Tags = context.Tags.ToArray()
            };

            foreach (var header in headers)
            {
                if (!string.Equals(header.Key, HeaderNames.Age, StringComparison.OrdinalIgnoreCase))
                {
                    context.CachedResponse.Headers[header.Key] = header.Value;
                }
            }

            return;
        }

        context.OutputCachingStream.DisableBuffering();
    }

    /// <summary>
    /// Stores the response body
    /// </summary>
    internal async ValueTask FinalizeCacheBodyAsync(OutputCachingContext context)
    {
        if (context.IsResponseCacheable && context.OutputCachingStream.BufferingEnabled)
        {
            var contentLength = context.HttpContext.Response.ContentLength;
            var cachedResponseBody = context.OutputCachingStream.GetCachedResponseBody();
            if (!contentLength.HasValue || contentLength == cachedResponseBody.Length
                || (cachedResponseBody.Length == 0
                    && HttpMethods.IsHead(context.HttpContext.Request.Method)))
            {
                var response = context.HttpContext.Response;
                // Add a content-length if required
                if (!response.ContentLength.HasValue && StringValues.IsNullOrEmpty(response.Headers.TransferEncoding))
                {
                    context.CachedResponse.Headers.ContentLength = cachedResponseBody.Length;
                }

                context.CachedResponse.Body = cachedResponseBody;
                _logger.ResponseCached();

                if (string.IsNullOrEmpty(context.CacheKey))
                {
                    throw new InvalidOperationException("Cache key must be defined");
                }

                await _cache.SetAsync(context.CacheKey, context.CachedResponse, context.CachedResponseValidFor);
            }
            else
            {
                _logger.ResponseContentLengthMismatchNotCached();
            }
        }
        else
        {
            _logger.LogResponseNotCached();
        }
    }

    /// <summary>
    /// Mark the response as started and set the response time if no response was started yet.
    /// </summary>
    /// <param name="context"></param>
    /// <returns><c>true</c> if the response was not started before this call; otherwise <c>false</c>.</returns>
    private bool OnStartResponse(OutputCachingContext context)
    {
        if (!context.ResponseStarted)
        {
            context.ResponseStarted = true;
            context.ResponseTime = _options.SystemClock.UtcNow;

            return true;
        }
        return false;
    }

    internal void StartResponse(OutputCachingContext context)
    {
        if (OnStartResponse(context))
        {
            FinalizeCacheHeaders(context);
        }
    }

    internal static void AddOutputCachingFeature(OutputCachingContext context)
    {
        if (context.HttpContext.Features.Get<IOutputCachingFeature>() != null)
        {
            throw new InvalidOperationException($"Another instance of {nameof(OutputCachingFeature)} already exists. Only one instance of {nameof(OutputCachingMiddleware)} can be configured for an application.");
        }

        context.HttpContext.Features.Set<IOutputCachingFeature>(new OutputCachingFeature(context));
    }

    internal void ShimResponseStream(OutputCachingContext context)
    {
        // Shim response stream
        context.OriginalResponseStream = context.HttpContext.Response.Body;
        context.OutputCachingStream = new OutputCachingStream(
            context.OriginalResponseStream,
            _options.MaximumBodySize,
            StreamUtilities.BodySegmentSize,
            () => StartResponse(context));
        context.HttpContext.Response.Body = context.OutputCachingStream;
    }

    internal static void RemoveOutputCachingFeature(HttpContext context) =>
        context.Features.Set<IOutputCachingFeature?>(null);

    internal static void UnshimResponseStream(OutputCachingContext context)
    {
        // Unshim response stream
        context.HttpContext.Response.Body = context.OriginalResponseStream;

        // Remove IOutputCachingFeature
        RemoveOutputCachingFeature(context.HttpContext);
    }

    internal static bool ContentIsNotModified(OutputCachingContext context)
    {
        var cachedResponseHeaders = context.CachedResponseHeaders;
        var ifNoneMatchHeader = context.HttpContext.Request.Headers.IfNoneMatch;

        if (!StringValues.IsNullOrEmpty(ifNoneMatchHeader))
        {
            if (ifNoneMatchHeader.Count == 1 && StringSegment.Equals(ifNoneMatchHeader[0], EntityTagHeaderValue.Any.Tag, StringComparison.OrdinalIgnoreCase))
            {
                context.Logger.NotModifiedIfNoneMatchStar();
                return true;
            }

            if (!StringValues.IsNullOrEmpty(cachedResponseHeaders.ETag)
                && EntityTagHeaderValue.TryParse(cachedResponseHeaders.ETag.ToString(), out var eTag)
                && EntityTagHeaderValue.TryParseList(ifNoneMatchHeader, out var ifNoneMatchEtags))
            {
                for (var i = 0; i < ifNoneMatchEtags.Count; i++)
                {
                    var requestETag = ifNoneMatchEtags[i];
                    if (eTag.Compare(requestETag, useStrongComparison: false))
                    {
                        context.Logger.NotModifiedIfNoneMatchMatched(requestETag);
                        return true;
                    }
                }
            }
        }
        else
        {
            var ifModifiedSince = context.HttpContext.Request.Headers.IfModifiedSince;
            if (!StringValues.IsNullOrEmpty(ifModifiedSince))
            {
                if (!HeaderUtilities.TryParseDate(cachedResponseHeaders.LastModified.ToString(), out var modified) &&
                    !HeaderUtilities.TryParseDate(cachedResponseHeaders.Date.ToString(), out modified))
                {
                    return false;
                }

                if (HeaderUtilities.TryParseDate(ifModifiedSince.ToString(), out var modifiedSince) &&
                    modified <= modifiedSince)
                {
                    context.Logger.NotModifiedIfModifiedSinceSatisfied(modified, modifiedSince);
                    return true;
                }
            }
        }

        return false;
    }

    // Normalize order and casing
    internal static StringValues GetOrderCasingNormalizedStringValues(StringValues stringValues)
    {
        if (stringValues.Count == 1)
        {
            return new StringValues(stringValues.ToString().ToUpperInvariant());
        }
        else
        {
            var originalArray = stringValues.ToArray();
            var newArray = new string[originalArray.Length];

            for (var i = 0; i < originalArray.Length; i++)
            {
                newArray[i] = originalArray[i]!.ToUpperInvariant();
            }

            // Since the casing has already been normalized, use Ordinal comparison
            Array.Sort(newArray, StringComparer.Ordinal);

            return new StringValues(newArray);
        }
    }

    internal static StringValues GetOrderCasingNormalizedDictionary(Dictionary<string, string> dictionary)
    {
        const char KeySubDelimiter = '\x1f';

        var newArray = new string[dictionary.Count];

        var i = 0;
        foreach (var (key, value) in dictionary)
        {
            newArray[i++] = $"{key.ToUpperInvariant()}{KeySubDelimiter}{value}";
        }

        // Since the casing has already been normalized, use Ordinal comparison
        Array.Sort(newArray, StringComparer.Ordinal);

        return new StringValues(newArray);
    }
}
