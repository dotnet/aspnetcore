// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

// Owns all coordination behind <see cref="CacheBoundary"/>: cache-key resolution, single-flight
// stampede protection, store interaction, cached-content deserialization, capture-writer creation and
// lifecycle, background persistence, and the associated logging. The CacheBoundary component and the
// CacheBoundaryTextWriter stay focused on rendering and writing respectively; everything else lives here.
internal sealed class CacheBoundaryService
{
    // HttpContext.Items key for the per-request set of cache keys that currently have an in-flight
    // creator (a CacheBoundary producing the entry during this render pass). Used so that a second
    // boundary which resolves to the same key in the same request does not wait on the first (which
    // would deadlock, since the creator's output is only produced during later HTML emission). On a warm
    // request there is no in-flight creator, so both boundaries simply hit the shared entry.
    private static readonly object _inFlightCreatorKeysItemKey = new();

    private static readonly JsonSerializerOptions _jsonOptions = ServerComponentSerializationSettings.JsonSerializationOptions;
    private static readonly ComponentParametersTypeCache _parametersTypeCache = new();

    private readonly ICacheBoundaryStore _store;
    private readonly ILogger<CacheBoundary> _logger;

    public CacheBoundaryService(ICacheBoundaryStore store, ILoggerFactory loggerFactory)
    {
        _store = store;
        _logger = loggerFactory.CreateLogger<CacheBoundary>();
    }

    // Phase 1 (component render): resolves the key and runs single-flight coordination, returning the
    // render state that describes whether this render is a cache hit, the single-flight creator, or a
    // same-request sibling that must render fresh. Returns null when caching is inactive for this render.
    public async Task<CacheBoundaryRenderState?> PrepareAsync(CacheBoundary boundary, HttpContext httpContext)
    {
        // Never serve cached content for a POST. Form submissions render live; the cache is neither read
        // nor written on a POST.
        if (!boundary.Enabled || HttpMethods.IsPost(httpContext.Request.Method))
        {
            return null;
        }

        var key = CacheBoundaryKeyResolver.ComputeKey(boundary, httpContext);
        var state = new CacheBoundaryRenderState(key, GetVaryBy(boundary));

        // Multiple CacheBoundary instances in one request can resolve to the same key (e.g. a reusable
        // component containing a CacheBoundary used more than once, or a loop without an explicit
        // CacheKey). They should share one cache entry. On a warm request both simply hit that entry. On
        // a cold request, however, the first becomes the single-flight creator and its captured output is
        // only produced during HTML emission (after this render pass reaches quiescence). A second
        // boundary that waited on it would therefore deadlock, so instead it renders fresh this one time;
        // the creator still populates the shared entry and every subsequent request serves both from it.
        var inFlightCreatorKeys = GetInFlightCreatorKeys(httpContext);
        if (inFlightCreatorKeys.Contains(key))
        {
            _logger.LogDebug(
                "Another CacheBoundary in the same request is currently creating cache key '{Key}'. Rendering this instance fresh to avoid waiting on the in-flight creator within a single render pass. It will share the cached entry on subsequent requests.",
                key);
            return state;
        }

        await ResolveOrBeginCreateAsync(boundary, state, inFlightCreatorKeys, httpContext.RequestAborted);
        return state;
    }

    // Phase 2a (renderer, before emitting the subtree): installs a capture writer when this boundary is
    // the single-flight creator. Otherwise returns false and the renderer writes directly to the output.
    public static bool TryBeginCapture(CacheBoundaryRenderState state, TextWriter realOutput, out TextWriter wrappedOutput)
    {
        if (!state.IsCreator || state.CaptureCompletion is null)
        {
            wrappedOutput = realOutput;
            return false;
        }

        var writer = new CacheBoundaryTextWriter(realOutput, state.VaryBy);
        writer.StartCapture();
        state.ActiveWriter = writer;
        wrappedOutput = writer;
        return true;
    }

    // Phase 2b (renderer, after emitting the subtree): finalizes the capture, hands the captured JSON to
    // the single-flight factory (which persists it), and observes persistence in the background.
    // Persistence failures are logged but do not fail the render.
    public void EndCapture(CacheBoundaryRenderState state)
    {
        var writer = state.ActiveWriter;
        if (writer is null)
        {
            return;
        }

        var completion = state.CaptureCompletion;
        var pending = state.PendingStoreTask;

        try
        {
            writer.StopCapture();
            completion?.TrySetResult(writer.GetJson());
        }
        catch (Exception ex)
        {
            completion?.TrySetException(ex);
            throw;
        }
        finally
        {
            state.ActiveWriter = null;
            state.CaptureCompletion = null;
            state.PendingStoreTask = null;
            if (pending is not null)
            {
                _ = ObserveCacheStorePersistAsync(state.Key, pending);
            }
        }
    }

    // Creates a writer that walks a non-creator boundary's subtree purely to surface validation errors
    // (for example a disallowed component used as a hole). It records nothing and produces no entry.
    public static CacheBoundaryTextWriter CreateValidationWriter(TextWriter output, CacheBoundaryVaryBy varyBy)
    {
        var writer = new CacheBoundaryTextWriter(output, varyBy);
        writer.StartValidation();
        return writer;
    }

    // Releases the single-flight reservation when a creator boundary is disposed before EndCapture runs
    // (for example because the render was abandoned), so that waiters re-elect a new creator.
    public static void OnBoundaryDisposed(CacheBoundaryRenderState state)
    {
        var completion = state.CaptureCompletion;
        if (completion is not null && !completion.Task.IsCompleted)
        {
            completion.TrySetCanceled();
        }

        state.ActiveWriter = null;
        state.CaptureCompletion = null;
        state.PendingStoreTask = null;
    }

    // Computes the vary-by dimensions active on the boundary from its parameters.
    public static CacheBoundaryVaryBy GetVaryBy(CacheBoundary boundary)
    {
        var result = CacheBoundaryVaryBy.None;

        if (!string.IsNullOrEmpty(boundary.VaryByQuery))
        {
            result |= CacheBoundaryVaryBy.Query;
        }

        if (!string.IsNullOrEmpty(boundary.VaryByRoute))
        {
            result |= CacheBoundaryVaryBy.Route;
        }

        if (!string.IsNullOrEmpty(boundary.VaryByHeader))
        {
            result |= CacheBoundaryVaryBy.Header;
        }

        if (!string.IsNullOrEmpty(boundary.VaryByCookie))
        {
            result |= CacheBoundaryVaryBy.Cookie;
        }

        if (boundary.VaryByUser is true)
        {
            result |= CacheBoundaryVaryBy.User;
        }

        if (boundary.VaryByCulture is true)
        {
            result |= CacheBoundaryVaryBy.Culture;
        }

        return result;
    }

    private async Task ResolveOrBeginCreateAsync(CacheBoundary boundary, CacheBoundaryRenderState state, HashSet<string> inFlightCreatorKeys, CancellationToken cancellationToken)
    {
        var captureCompletion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var factoryStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        state.CaptureCompletion = captureCompletion;

        var options = new CacheStoreOptions
        {
            ExpiresAfter = boundary.ExpiresAfter,
            ExpiresOn = boundary.ExpiresOn,
            ExpiresSliding = boundary.ExpiresSliding,
            Priority = boundary.Priority,
        };

        var inflight = _store.GetOrCreateAsync(
            state.Key,
            async ct =>
            {
                // We won the single-flight race; the renderer's live walk will produce the JSON. Mark
                // this key as having an in-flight creator (synchronously, before this method suspends) so
                // any same-request sibling that resolves to it renders fresh instead of waiting on us.
                state.IsCreator = true;
                inFlightCreatorKeys.Add(state.Key);
                factoryStarted.TrySetResult();
                return await captureCompletion.Task.WaitAsync(ct);
            },
            options,
            cancellationToken).AsTask();

        // Wait for whichever happens first: the cached value is available (hit or someone else's factory
        // completed) OR our factory got invoked (we're the creator).
        var first = await Task.WhenAny(inflight, factoryStarted.Task);
        if (first == inflight)
        {
            state.CachedContent = DeserializeCachedContent(await inflight);
        }
        else
        {
            state.PendingStoreTask = inflight;
        }
    }

    private RenderFragment? DeserializeCachedContent(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<SerializedRenderFragment>(json, _jsonOptions);
            if (payload is null || payload.Nodes.Count == 0)
            {
                return null;
            }

            return RenderFragmentSerializer.Deserialize(payload.Nodes, _jsonOptions, _parametersTypeCache);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to restore CacheBoundary from cached data. Falling back to fresh render.");
            return null;
        }
    }

    private async Task ObserveCacheStorePersistAsync(string key, Task<string> pending)
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
            _logger.LogWarning(ex, "Failed to persist CacheBoundary entry for key '{Key}'.", key);
        }
    }

    private static HashSet<string> GetInFlightCreatorKeys(HttpContext httpContext)
    {
        if (httpContext.Items[_inFlightCreatorKeysItemKey] is not HashSet<string> keys)
        {
            keys = new HashSet<string>(StringComparer.Ordinal);
            httpContext.Items[_inFlightCreatorKeysItemKey] = keys;
        }

        return keys;
    }
}
