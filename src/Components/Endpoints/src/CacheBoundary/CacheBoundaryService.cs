// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Components.HotReload;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

// Owns all coordination behind <see cref="CacheBoundary"/>: cache-key resolution, single-flight
// stampede protection, store interaction, cached-content deserialization, capture-writer creation and
// lifecycle, hole-policy decisions, background persistence, and the associated logging. The CacheBoundary
// component and the CacheBoundaryTextWriter stay focused on rendering and writing respectively;
// everything else lives here.
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

    // Caches the [CacheBoundaryPolicy] lookup per component type. Cleared on hot reload so attribute edits
    // take effect without restarting.
    private static readonly ConcurrentDictionary<Type, CacheBoundaryPolicyAttribute?> _policyByComponentType = new();

    static CacheBoundaryService()
    {
        if (HotReloadManager.IsSupported)
        {
            HotReloadManager.Default.OnDeltaApplied += _policyByComponentType.Clear;
        }
    }

    private readonly ICacheBoundaryStore _store;
    private readonly ILogger<CacheBoundary> _logger;

    public CacheBoundaryService(ICacheBoundaryStore store, ILoggerFactory loggerFactory)
    {
        _store = store;
        _logger = loggerFactory.CreateLogger<CacheBoundary>();
    }

    // Determines whether <paramref name="componentType"/> is a "hole" inside a CacheBoundary varying by
    // <paramref name="varyBy"/>: a component annotated with [CacheBoundaryPolicy] whose VaryBy dimensions
    // are not all covered. Throws when the component is annotated with Disallow and is not covered.
    public static bool IsHoleComponent(Type componentType, CacheBoundaryVaryBy varyBy)
    {
        var attr = _policyByComponentType.GetOrAdd(
            componentType,
            static type => type.GetCustomAttribute<CacheBoundaryPolicyAttribute>(inherit: true));

        if (attr is null)
        {
            return false;
        }

        // A VaryBy of None means the component is never safe to include in the
        // cached output regardless of what dimensions the boundary varies by.
        var varyByMatches = attr.VaryBy != CacheBoundaryVaryBy.None && (attr.VaryBy & varyBy) == attr.VaryBy;

        if (attr.Disallow && !varyByMatches)
        {
            throw new InvalidOperationException(
                $"Component '{componentType.FullName}' cannot be used inside a CacheBoundary in its current configuration. " +
                $"It is annotated with [CacheBoundaryPolicy(Disallow = true, VaryBy = {attr.VaryBy})] " +
                $"because its rendered output depends on per-request state that cannot be safely captured into a cache entry and replayed on later requests. " +
                (attr.VaryBy != CacheBoundaryVaryBy.None
                    ? $"To use it inside a CacheBoundary, configure the boundary so that it varies by all of the following dimensions: {attr.VaryBy}. "
                    : "") +
                $"Alternatively, move this component outside the CacheBoundary, or wrap it in a component marked with [CacheBoundaryPolicy] so that its subtree is excluded from caching.");
        }

        // If Disallow is true we only reach here when varyByMatches is true (safe to cache).
        // If Disallow is false, it's a hole only when VaryBy dimensions aren't covered.
        return !varyByMatches;
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
        var state = new CacheBoundaryRenderState(key, GetVaryBy(boundary))
        {
            // Default to rendering the boundary's own content; the hit path below overrides this with the
            // cached output. This way the component always renders state.Content without branching.
            Content = boundary.ChildContent,
        };

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

    // Phase 2a (renderer, before emitting the subtree): decides how to wrap the output for this boundary
    // and returns true when it installed a wrapper the renderer should write into.
    // - Single-flight creator: returns a capture writer; the renderer calls <see cref="EndCapture"/>
    //   afterward to produce the entry.
    // - Any other boundary not already inside a capture writer: returns a validation-only writer that
    //   surfaces hole/policy errors without producing an entry.
    // - A boundary already inside a capture writer (nested): returns false and the renderer writes directly.
    public static bool TryBeginWrite(CacheBoundaryRenderState? state, CacheBoundary boundary, TextWriter output, out TextWriter wrappedOutput)
    {
        if (state is { IsCreator: true, CaptureCompletion: not null })
        {
            var captureWriter = new CacheBoundaryTextWriter(output, state.VaryBy);
            captureWriter.StartCapture();
            state.ActiveWriter = captureWriter;
            wrappedOutput = captureWriter;
            return true;
        }

        if (output is not CacheBoundaryTextWriter)
        {
            var validationWriter = new CacheBoundaryTextWriter(output, GetVaryBy(boundary));
            validationWriter.StartValidation();
            wrappedOutput = validationWriter;
            return true;
        }

        wrappedOutput = output;
        return false;
    }

    // Phase 2b (renderer, after emitting the subtree): finalizes a capture, hands the captured JSON to the
    // single-flight factory (which persists it), and observes persistence in the background. No-ops when
    // there was no capture (a validation-only writer, a nested boundary, or caching inactive), so the
    // renderer can call it unconditionally. Persistence failures are logged but do not fail the render.
    public void EndCapture(CacheBoundaryRenderState? state)
    {
        var writer = state?.ActiveWriter;
        if (state is null || writer is null)
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
            // Cache hit (or waiter): render the stored output. A deserialization failure falls back to the
            // boundary's content while still treating this as a hit (no capture), matching the behavior of
            // a render that produced no cacheable entry.
            state.IsCacheHit = true;
            state.Content = DeserializeCachedContent(await inflight) ?? boundary.ChildContent;
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
