// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// A component that caches the rendered HTML of its child content during
/// server-side rendering (SSR). On cache hit, child components are not
/// instantiated or rendered.
/// </summary>
public sealed class CacheBoundary : ComponentBase, IDisposable
{
    private static readonly ComponentParametersTypeCache _parametersTypeCache = new();
    private static readonly JsonSerializerOptions _jsonOptions = ServerComponentSerializationSettings.JsonSerializationOptions;

    /// <summary>
    /// Gets or sets the content to be cached.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets an explicit cache key for disambiguation when multiple
    /// <see cref="CacheBoundary"/> instances share the same component ancestor.
    /// </summary>
    [Parameter]
    public string? CacheKey { get; set; }

    /// <summary>
    /// Gets or sets whether caching is enabled. Defaults to <c>true</c>.
    /// </summary>
    [Parameter]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets how long after creation the cache entry should be evicted.
    /// </summary>
    [Parameter]
    public TimeSpan? ExpiresAfter { get; set; }

    /// <summary>
    /// Gets or sets the absolute <see cref="DateTimeOffset"/> when the cache entry should be evicted.
    /// </summary>
    [Parameter]
    public DateTimeOffset? ExpiresOn { get; set; }

    /// <summary>
    /// Gets or sets how long after last access the cache entry should be evicted.
    /// Not supported when the cache boundary store is backed by <c>HybridCache</c>.
    /// </summary>
    [Parameter]
    public TimeSpan? ExpiresSliding { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="CacheItemPriority"/> policy for the cache entry.
    /// </summary>
    [Parameter]
    public CacheItemPriority? Priority { get; set; }

    /// <summary>
    /// Gets or sets a comma-separated list of query string parameter names to vary the cache by.
    /// </summary>
    [Parameter]
    public string? VaryByQuery { get; set; }

    /// <summary>
    /// Gets or sets a comma-separated list of route parameter names to vary the cache by.
    /// </summary>
    [Parameter]
    public string? VaryByRoute { get; set; }

    /// <summary>
    /// Gets or sets a comma-separated list of HTTP header names to vary the cache by.
    /// </summary>
    [Parameter]
    public string? VaryByHeader { get; set; }

    /// <summary>
    /// Gets or sets a comma-separated list of cookie names to vary the cache by.
    /// </summary>
    [Parameter]
    public string? VaryByCookie { get; set; }

    /// <summary>
    /// Gets or sets whether to vary the cache by the authenticated user identity.
    /// </summary>
    [Parameter]
    public bool? VaryByUser { get; set; }

    /// <summary>
    /// Gets or sets whether to vary the cache by the current culture.
    /// </summary>
    [Parameter]
    public bool? VaryByCulture { get; set; }

    /// <summary>
    /// Gets or sets a custom string value to vary the cache by.
    /// </summary>
    [Parameter]
    public string? VaryBy { get; set; }

    [Inject] internal ICacheBoundaryStore? CacheStore { get; set; }
    [CascadingParameter] internal HttpContext? HttpContext { get; set; }
    internal Func<string>? TreePositionKeyFactory { get; set; }
    internal string? TreePositionKey => TreePositionKeyFactory?.Invoke();
    internal string? ResolvedCacheKey { get; private set; }
    internal string? CachedData { get; private set; }

    // Set on cache miss when caching is active. Wraps ChildContent so the live render populates frame
    // captures that the cache can read at hole-emission time (and recurses into nested RenderFragments).
    internal RenderFragmentCapture? ChildContentCapture { get; private set; }

    // Tracks the active capture writer between TryBeginCacheCapture and EndCacheCapture. Non-null
    // only while the renderer is walking this boundary's subtree on a cache miss.
    private CacheBoundaryTextWriter? _activeCaptureWriter;

    // Single-flight coordination. When this boundary is the creator for a key, _isCreator is true,
    // _captureCompletion is the TCS that the renderer fulfils via EndCacheCapture, and
    // _pendingCacheStoreTask is the in-flight GetOrCreateAsync that completes once the store persists.
    private bool _isCreator;
    private TaskCompletionSource<string>? _captureCompletion;
    private Task<string>? _pendingCacheStoreTask;

    internal CacheBoundaryVaryBy GetVaryByOptions()
    {
        var result = CacheBoundaryVaryBy.None;

        if (!string.IsNullOrEmpty(VaryByQuery))
        {
            result |= CacheBoundaryVaryBy.Query;
        }

        if (!string.IsNullOrEmpty(VaryByRoute))
        {
            result |= CacheBoundaryVaryBy.Route;
        }

        if (!string.IsNullOrEmpty(VaryByHeader))
        {
            result |= CacheBoundaryVaryBy.Header;
        }

        if (!string.IsNullOrEmpty(VaryByCookie))
        {
            result |= CacheBoundaryVaryBy.Cookie;
        }

        if (VaryByUser is true)
        {
            result |= CacheBoundaryVaryBy.User;
        }

        if (VaryByCulture is true)
        {
            result |= CacheBoundaryVaryBy.Culture;
        }

        return result;
    }

    /// <inheritdoc/>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        // Reset per-render state.
        ResolvedCacheKey = null;
        CachedData = null;
        ChildContentCapture = null;
        _isCreator = false;
        _captureCompletion = null;
        _pendingCacheStoreTask = null;

        if (Enabled && CacheStore is not null && HttpContext is { } httpContext)
        {
            ResolvedCacheKey = CacheBoundaryKeyResolver.ComputeKey(this, httpContext);
            await ResolveOrBeginCreateAsync(httpContext.RequestAborted);
        }

        await base.SetParametersAsync(ParameterView.Empty);
    }

    private async Task ResolveOrBeginCreateAsync(CancellationToken cancellationToken)
    {
        var captureCompletion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var factoryStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _captureCompletion = captureCompletion;

        var options = new CacheStoreOptions
        {
            ExpiresAfter = ExpiresAfter,
            ExpiresOn = ExpiresOn,
            ExpiresSliding = ExpiresSliding,
            Priority = Priority,
        };

        var inflight = CacheStore!.GetOrCreateAsync(
            ResolvedCacheKey!,
            async ct =>
            {
                // We won the single-flight race; the renderer's live walk will produce the JSON.
                _isCreator = true;
                factoryStarted.TrySetResult();
                return await captureCompletion.Task.WaitAsync(ct);
            },
            options,
            cancellationToken).AsTask();

        // Wait for whichever happens first: the cached value is available (hit or someone else's
        // factory completed) OR our factory got invoked (we're the creator).
        var first = await Task.WhenAny(inflight, factoryStarted.Task);
        if (first == inflight)
        {
            CachedData = await inflight;
        }
        else
        {
            _pendingCacheStoreTask = inflight;
        }
    }

    /// <inheritdoc/>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (TryDeserializeCachedNodes(out var nodes))
        {
            ChildContentCapture = null;
            // Cache hit (or waiter): invoke the deserialized RenderFragment straight into the live builder.
            RenderFragmentSerializer.Deserialize(nodes!, _jsonOptions, _parametersTypeCache)(builder);
            return;
        }

        CachedData = null;

        if (_isCreator && ChildContent is { } childContent)
        {
            ChildContentCapture = new RenderFragmentCapture(childContent);
            builder.AddContent(0, (RenderFragment)ChildContentCapture.Invoke);
        }
        else
        {
            ChildContentCapture = null;
            builder.AddContent(0, ChildContent);
        }
    }

    // Invoked by EndpointHtmlRenderer when it is about to emit HTML for this boundary's subtree.
    // Returns true with a wrapped writer only when this boundary is the single-flight creator for
    // its key. On a hit, waiter, or when caching is inactive, returns false and the renderer should
    // write directly to <paramref name="realOutput"/>.
    internal bool TryBeginCacheCapture(TextWriter realOutput, out TextWriter wrappedOutput)
    {
        if (!_isCreator || _captureCompletion is null)
        {
            wrappedOutput = realOutput;
            return false;
        }

        var writer = new CacheBoundaryTextWriter(realOutput, GetVaryByOptions(), ChildContentCapture);
        writer.StartCapture();
        _activeCaptureWriter = writer;
        wrappedOutput = writer;
        return true;
    }

    // Invoked by EndpointHtmlRenderer after the subtree walk completes. Finalizes the capture and
    // fulfils the single-flight factory's task, which causes the store to persist the entry.
    // Cache-store persistence is observed in the background; failures are logged but do not fail
    // the render.
    internal void EndCacheCapture()
    {
        if (_activeCaptureWriter is null)
        {
            return;
        }

        var completion = _captureCompletion;
        var pending = _pendingCacheStoreTask;

        try
        {
            _activeCaptureWriter.StopCapture();
            var json = _activeCaptureWriter.GetJson(GetSerializationLogger());
            completion?.TrySetResult(json);
        }
        catch (Exception ex)
        {
            completion?.TrySetException(ex);
            throw;
        }
        finally
        {
            _activeCaptureWriter = null;
            _captureCompletion = null;
            _pendingCacheStoreTask = null;
            if (pending is not null)
            {
                _ = ObserveCacheStorePersistAsync(pending);
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        var completion = _captureCompletion;
        if (completion is not null && !completion.Task.IsCompleted)
        {
            completion.TrySetCanceled();
        }

        _activeCaptureWriter = null;
        _captureCompletion = null;
        _pendingCacheStoreTask = null;
    }

    private async Task ObserveCacheStorePersistAsync(Task<string> pending)
    {
        try
        {
            await pending;
        }
        catch (OperationCanceledException)
        {
            // Request aborted while persisting; nothing to log.
        }
        catch (Exception ex)
        {
            GetLogger()?.LogWarning(ex, "Failed to persist CacheBoundary entry for key '{Key}'.", ResolvedCacheKey);
        }
    }

    private ILogger? GetLogger()
        => HttpContext?.RequestServices.GetService<ILoggerFactory>()?.CreateLogger<CacheBoundary>();

    private ILogger GetSerializationLogger()
        => HttpContext!.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(RenderFragmentSerializer).FullName!);

    private bool TryDeserializeCachedNodes(out List<RenderTreeNode>? nodes)
    {
        nodes = null;

        if (string.IsNullOrEmpty(CachedData))
        {
            return false;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<SerializedRenderFragment>(CachedData, _jsonOptions);
            if (payload is null || payload.Nodes.Count == 0)
            {
                return false;
            }
            nodes = payload.Nodes;
            return true;
        }
        catch (Exception ex)
        {
            GetLogger()?.LogWarning(ex, "Failed to restore CacheBoundary from cached data. Falling back to fresh render.");
            return false;
        }
    }
}
