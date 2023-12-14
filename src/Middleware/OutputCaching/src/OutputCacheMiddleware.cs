// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
internal sealed class OutputCacheMiddleware
{
    // see https://tools.ietf.org/html/rfc7232#section-4.1
    private static readonly string[] HeadersToIncludeIn304 =
        new[] { "Cache-Control", "Content-Location", "Date", "ETag", "Expires", "Vary" };

    private readonly RequestDelegate _next;
    private readonly OutputCacheOptions _options;
    private readonly ILogger _logger;
    private readonly IOutputCacheStore _store;
    private readonly IOutputCacheKeyProvider _keyProvider;
    private readonly WorkDispatcher<string, OutputCacheEntry?> _outputCacheEntryDispatcher;
    private readonly WorkDispatcher<string, OutputCacheEntry?> _requestDispatcher;

    /// <summary>
    /// Creates a new <see cref="OutputCacheMiddleware"/>.
    /// </summary>
    /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
    /// <param name="options">The options for this middleware.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> used for logging.</param>
    /// <param name="outputCache">The <see cref="IOutputCacheStore"/> store.</param>
    /// <param name="poolProvider">The <see cref="ObjectPoolProvider"/> used for creating <see cref="ObjectPool"/> instances.</param>
    public OutputCacheMiddleware(
        RequestDelegate next,
        IOptions<OutputCacheOptions> options,
        ILoggerFactory loggerFactory,
        IOutputCacheStore outputCache,
        ObjectPoolProvider poolProvider
        )
        : this(
            next,
            options,
            loggerFactory,
            outputCache,
            new OutputCacheKeyProvider(poolProvider, options))
    { }

    // for testing
    internal OutputCacheMiddleware(
        RequestDelegate next,
        IOptions<OutputCacheOptions> options,
        ILoggerFactory loggerFactory,
        IOutputCacheStore cache,
        IOutputCacheKeyProvider keyProvider)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(keyProvider);

        _next = next;
        _options = options.Value;
        _logger = loggerFactory.CreateLogger<OutputCacheMiddleware>();
        _store = cache;
        _keyProvider = keyProvider;
        _outputCacheEntryDispatcher = new();
        _requestDispatcher = new();
    }

    /// <summary>
    /// Invokes the logic of the middleware.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    /// <returns>A <see cref="Task"/> that completes when the middleware has completed processing.</returns>
    public Task Invoke(HttpContext httpContext)
    {
        // Skip the middleware if there is no policy for the current request
        if (!TryGetRequestPolicies(httpContext, out var policies))
        {
            return _next(httpContext);
        }

        return InvokeAwaited(httpContext, policies);
    }

    private async Task InvokeAwaited(HttpContext httpContext, IReadOnlyList<IOutputCachePolicy> policies)
    {
        var context = new OutputCacheContext { HttpContext = httpContext };

        // Add IOutputCacheFeature
        AddOutputCacheFeature(context);
        bool hasException = false;
        try
        {
            foreach (var policy in policies)
            {
                await policy.CacheRequestAsync(context, httpContext.RequestAborted);
            }

            // Should we attempt any caching logic?
            if (context.EnableOutputCaching)
            {
                // Can this request be served from cache?
                if (context.AllowCacheLookup)
                {
                    bool served = await TryServeFromCacheAsync(context, policies);

                    // release even if not served due to failing conditions
                    // (note that this is *in addition* to the finally, because execute
                    // may update this with another valid response later; this nulls
                    // out the value after recycle, and is fine to call multiple times)
                    context.ReleaseCachedResponse();

                    if (served)
                    {
                        // note: no cached-response exposed here (so no need to recycle)
                        return;
                    }
                }

                // Should we store the response to this request?
                if (context.AllowCacheStorage)
                {
                    CreateCacheKey(context);

                    // It is also a pre-condition to response locking

                    var executed = false;

                    if (context.AllowLocking)
                    {
                        var cacheEntry = await _requestDispatcher.ScheduleAsync(context.CacheKey, key => ExecuteResponseAsync());

                        // The current request was processed, nothing more to do
                        if (executed)
                        {
                            return;
                        }

                        // If the result was processed by another request, try to serve it from cache entry (no lookup)
                        if (await TryServeCachedResponseAsync(context, cacheEntry, policies))
                        {
                            return;
                        }

                        // If the cache entry couldn't be served, continue to processing the request as usual
                    }

                    await ExecuteResponseAsync();
                    return;

                    async Task<OutputCacheEntry?> ExecuteResponseAsync()
                    {
                        // Hook up to listen to the response stream
                        ShimResponseStream(context);

                        try
                        {
                            await _next(httpContext);

                            // The next middleware might change the policy
                            foreach (var policy in policies)
                            {
                                await policy.ServeResponseAsync(context, httpContext.RequestAborted);
                            }

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

                        // If the policies prevented this response from being cached we can't reuse it for other
                        // pending requests
                        if (!context.AllowCacheStorage)
                        {
                            context.ReleaseCachedResponse();
                        }
                        return context.CachedResponse;
                    }
                }
            }

            await _next(httpContext);
        }
        catch
        {
            // avoid recycling in unknown outcomes, especially re concurrent buffer access thru cancellation
            hasException = true;
            throw;
        }
        finally
        {
            if (!hasException)
            {
                context.ReleaseCachedResponse();
            }
            RemoveOutputCacheFeature(httpContext);
        }
    }

    internal bool TryGetRequestPolicies(HttpContext httpContext, out IReadOnlyList<IOutputCachePolicy> policies)
    {
        policies = Array.Empty<IOutputCachePolicy>();
        List<IOutputCachePolicy>? result = null;

        if (_options.BasePolicies != null)
        {
            result = new();
            result.AddRange(_options.BasePolicies);
        }

        var metadata = httpContext.GetEndpoint()?.Metadata;

        var policy = metadata?.GetMetadata<IOutputCachePolicy>();

        if (policy != null)
        {
            result ??= new();
            result.Add(policy);
        }

        var attribute = metadata?.GetMetadata<OutputCacheAttribute>();

        if (attribute != null)
        {
            result ??= new();
            result.Add(attribute.BuildPolicy());
        }

        if (result != null)
        {
            policies = result;
            return true;
        }

        return false;
    }

    internal async Task<bool> TryServeCachedResponseAsync(OutputCacheContext context, OutputCacheEntry? cacheEntry, IReadOnlyList<IOutputCachePolicy> policies)
    {
        if (cacheEntry == null)
        {
            return false;
        }

        context.CachedResponse = cacheEntry;
        context.ResponseTime = _options.TimeProvider.GetUtcNow();
        var cacheEntryAge = context.ResponseTime.Value - context.CachedResponse.Created;
        context.CachedEntryAge = cacheEntryAge > TimeSpan.Zero ? cacheEntryAge : TimeSpan.Zero;

        foreach (var policy in policies)
        {
            await policy.ServeFromCacheAsync(context, context.HttpContext.RequestAborted);
        }

        context.IsCacheEntryFresh = true;

        // Validate expiration
        if (context.CachedEntryAge <= TimeSpan.Zero)
        {
            _logger.ExpirationExpiresExceeded(context.ResponseTime!.Value);
            context.IsCacheEntryFresh = false;
        }

        var cachedResponse = context.CachedResponse;
        if (context.IsCacheEntryFresh)
        {
            // Check conditional request rules
            if (ContentIsNotModified(context))
            {
                _logger.NotModifiedServed();
                context.HttpContext.Response.StatusCode = StatusCodes.Status304NotModified;

                foreach (var key in HeadersToIncludeIn304)
                {
                    if (cachedResponse.TryFindHeader(key, out var values))
                    {
                        context.HttpContext.Response.Headers[key] = values;
                    }
                }
            }
            else
            {
                var response = context.HttpContext.Response;
                // Copy the cached status code and response headers
                response.StatusCode = cachedResponse.StatusCode;

                cachedResponse.CopyHeadersTo(response.Headers);

                // Note: int64 division truncates result and errors may be up to 1 second. This reduction in
                // accuracy of age calculation is considered appropriate since it is small compared to clock
                // skews and the "Age" header is an estimate of the real age of cached content.
                response.Headers.Age = HeaderUtilities.FormatNonNegativeInt64(context.CachedEntryAge.Ticks / TimeSpan.TicksPerSecond);

                // Copy the cached response body
                var body = context.CachedResponse.Body;

                if (!body.IsEmpty)
                {
                    try
                    {
                        await context.CachedResponse.CopyToAsync(response.BodyWriter, context.HttpContext.RequestAborted);
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

    internal async Task<bool> TryServeFromCacheAsync(OutputCacheContext cacheContext, IReadOnlyList<IOutputCachePolicy> policies)
    {
        CreateCacheKey(cacheContext);

        // If the cache key can't be computed skip it
        if (string.IsNullOrEmpty(cacheContext.CacheKey))
        {
            return false;
        }

        // Locking cache lookups by default
        // TODO: should it be part of the cache implementations or can we assume all caches would benefit from it?
        // It makes sense for caches that use IO (disk, network) or need to deserialize the state but could also be a global option
        OutputCacheEntry? cacheEntry;
        try
        {
            cacheEntry = await _outputCacheEntryDispatcher.ScheduleAsync(cacheContext.CacheKey, (Store: _store, CacheContext: cacheContext), static async (key, state) => await OutputCacheEntryFormatter.GetAsync(key, state.Store, state.CacheContext.HttpContext.RequestAborted));
        }
        catch (OperationCanceledException)
        {
            // don't report as failure
            cacheEntry = null;
        }
        catch (Exception ex)
        {
            _logger.UnableToQueryOutputCache(ex);
            cacheEntry = null;
        }

        if (cacheEntry is not null && await TryServeCachedResponseAsync(cacheContext, cacheEntry, policies))
        {
            return true;
        }

        if (HeaderUtilities.ContainsCacheDirective(cacheContext.HttpContext.Request.Headers.CacheControl, CacheControlHeaderValue.OnlyIfCachedString))
        {
            _logger.GatewayTimeoutServed();
            cacheContext.HttpContext.Response.StatusCode = StatusCodes.Status504GatewayTimeout;
            return true;
        }

        _logger.NoResponseServed();
        return false;
    }

    internal void CreateCacheKey(OutputCacheContext context)
    {
        if (!string.IsNullOrEmpty(context.CacheKey))
        {
            return;
        }

        context.CacheKey = _keyProvider.CreateStorageKey(context);
    }

    /// <summary>
    /// Finalize cache headers.
    /// </summary>
    /// <param name="context"></param>
    internal void FinalizeCacheHeaders(OutputCacheContext context)
    {
        if (context.AllowCacheStorage)
        {
            // Create the cache entry now
            var response = context.HttpContext.Response;
            var headers = response.Headers;

            context.CachedResponseValidFor = context.ResponseExpirationTimeSpan ?? _options.DefaultExpirationTimeSpan;

            // Setting the date on the raw response headers.
            headers.Date = HeaderUtilities.FormatDate(context.ResponseTime!.Value);

            // Store the response on the state
            var cacheEntry = new OutputCacheEntry(context.ResponseTime!.Value, response.StatusCode)
                .CopyHeadersFrom(headers);
            context.CachedResponse = cacheEntry;

            return;
        }

        context.OutputCacheStream.DisableBuffering();
    }

    /// <summary>
    /// Stores the response body
    /// </summary>
    internal async ValueTask FinalizeCacheBodyAsync(OutputCacheContext context)
    {
        if (context.AllowCacheStorage && context.OutputCacheStream.BufferingEnabled
            && context.CachedResponse is not null)
        {
            // If AllowCacheLookup is false, the cache key was not created
            CreateCacheKey(context);

            var contentLength = context.HttpContext.Response.ContentLength;
            var cachedResponseBody = context.OutputCacheStream.GetCachedResponseBody();

            if (!contentLength.HasValue || contentLength == cachedResponseBody.Length
                || (cachedResponseBody.Length == 0
                    && HttpMethods.IsHead(context.HttpContext.Request.Method)))
            {
                // transfer lifetime from the buffer to the cached response
                context.CachedResponse.SetBody(cachedResponseBody, recycleBuffers: true);

                if (string.IsNullOrEmpty(context.CacheKey))
                {
                    _logger.ResponseNotCached();
                }
                else
                {
                    _logger.ResponseCached();

                    await OutputCacheEntryFormatter.StoreAsync(context.CacheKey, context.CachedResponse, context.Tags, context.CachedResponseValidFor,
                        _store, _logger, context.HttpContext.RequestAborted);
                }
            }
            else
            {
                _logger.ResponseContentLengthMismatchNotCached();
            }
        }
        else
        {
            _logger.ResponseNotCached();
        }
    }

    /// <summary>
    /// Mark the response as started and set the response time if no response was started yet.
    /// </summary>
    /// <param name="context"></param>
    /// <returns><c>true</c> if the response was not started before this call; otherwise <c>false</c>.</returns>
    private bool OnStartResponse(OutputCacheContext context)
    {
        if (!context.ResponseStarted)
        {
            context.ResponseStarted = true;
            context.ResponseTime = _options.TimeProvider.GetUtcNow();

            return true;
        }
        return false;
    }

    internal void StartResponse(OutputCacheContext context)
    {
        if (OnStartResponse(context))
        {
            FinalizeCacheHeaders(context);
        }
    }

    internal static void AddOutputCacheFeature(OutputCacheContext context)
    {
        if (context.HttpContext.Features.Get<IOutputCacheFeature>() != null)
        {
            throw new InvalidOperationException($"Another instance of {nameof(OutputCacheFeature)} already exists. Only one instance of {nameof(OutputCacheMiddleware)} can be configured for an application.");
        }

        context.HttpContext.Features.Set<IOutputCacheFeature>(new OutputCacheFeature(context));
    }

    internal void ShimResponseStream(OutputCacheContext context)
    {
        // Shim response stream
        context.OriginalResponseStream = context.HttpContext.Response.Body;
        context.OutputCacheStream = new OutputCacheStream(
            context.OriginalResponseStream,
            _options.MaximumBodySize,
            StreamUtilities.BodySegmentSize,
            () => StartResponse(context));
        context.HttpContext.Response.Body = context.OutputCacheStream;
    }

    internal static void RemoveOutputCacheFeature(HttpContext context) =>
        context.Features.Set<IOutputCacheFeature?>(null);

    internal static void UnshimResponseStream(OutputCacheContext context)
    {
        // Unshim response stream
        context.HttpContext.Response.Body = context.OriginalResponseStream;

        // Remove IOutputCachingFeature
        RemoveOutputCacheFeature(context.HttpContext);
    }

    internal bool ContentIsNotModified(OutputCacheContext context)
    {
        var cachedResponse = context.CachedResponse;
        var ifNoneMatchHeader = context.HttpContext.Request.Headers.IfNoneMatch;

        if (cachedResponse is null)
        {
            return false;
        }

        if (!StringValues.IsNullOrEmpty(ifNoneMatchHeader))
        {
            if (ifNoneMatchHeader.Count == 1 && StringSegment.Equals(ifNoneMatchHeader[0], EntityTagHeaderValue.Any.Tag, StringComparison.OrdinalIgnoreCase))
            {
                _logger.NotModifiedIfNoneMatchStar();
                return true;
            }
            
            if (cachedResponse.TryFindHeader(HeaderNames.ETag, out var raw)
                && !StringValues.IsNullOrEmpty(raw)
                && EntityTagHeaderValue.TryParse(raw.ToString(), out var eTag)
                && EntityTagHeaderValue.TryParseList(ifNoneMatchHeader, out var ifNoneMatchETags))
            {
                for (var i = 0; i < ifNoneMatchETags?.Count; i++)
                {
                    var requestETag = ifNoneMatchETags[i];
                    if (eTag.Compare(requestETag, useStrongComparison: false))
                    {
                        _logger.NotModifiedIfNoneMatchMatched(requestETag);
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
                if (!HeaderUtilities.TryParseDate(cachedResponse.FindHeader(HeaderNames.LastModified).ToString(), out var modified) &&
                    !HeaderUtilities.TryParseDate(cachedResponse.FindHeader(HeaderNames.Date).ToString(), out modified))
                {
                    return false;
                }

                if (HeaderUtilities.TryParseDate(ifModifiedSince.ToString(), out var modifiedSince) &&
                    modified <= modifiedSince)
                {
                    _logger.NotModifiedIfModifiedSinceSatisfied(modified, modifiedSince);
                    return true;
                }
            }
        }

        return false;
    }
}
