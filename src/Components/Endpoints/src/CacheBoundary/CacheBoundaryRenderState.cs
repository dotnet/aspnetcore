// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints;

// Per-render coordination state for a single CacheBoundary instance. Created and driven by
// CacheBoundaryService; spans the two phases of a cache miss (the component render that begins the
// single-flight, and the later HTML emission that produces and persists the entry). The CacheBoundary
// component only holds a reference to it, while all logic that reads or mutates it lives in the service.
internal sealed class CacheBoundaryRenderState
{
    public CacheBoundaryRenderState(string key, CacheBoundaryVaryBy varyBy)
    {
        Key = key;
        VaryBy = varyBy;
    }

    // The resolved cache key for this render.
    public string Key { get; }

    // The vary-by dimensions active on the boundary, used to detect holes during capture.
    public CacheBoundaryVaryBy VaryBy { get; }

    // Non-null on a cache hit (or waiter): the deserialized cached content ready to render.
    public RenderFragment? CachedContent { get; set; }

    // True when this boundary won the single-flight race and must capture its output.
    public bool IsCreator { get; set; }

    // Completed by the service once HTML emission finishes; hands the captured JSON to the store factory.
    public TaskCompletionSource<string>? CaptureCompletion { get; set; }

    // The in-flight store GetOrCreateAsync task observed in the background once capture completes.
    public Task<string>? PendingStoreTask { get; set; }

    // The capture writer installed for the creator between TryBeginCapture and EndCapture.
    public CacheBoundaryTextWriter? ActiveWriter { get; set; }
}
