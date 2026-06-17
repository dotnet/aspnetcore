// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed class CacheBoundaryRenderState
{
    public CacheBoundaryRenderState(string key, CacheBoundaryVaryBy varyBy)
    {
        Key = key;
        VaryBy = varyBy;
    }

    public string Key { get; }

    public CacheBoundaryVaryBy VaryBy { get; }

    public RenderFragment? Content { get; set; }

    public bool IsCacheHit { get; set; }

    public bool IsCreator { get; set; }

    public TaskCompletionSource<string>? CaptureCompletion { get; set; }

    public Task<string>? PendingStoreTask { get; set; }

    public CacheBoundaryTextWriter? ActiveWriter { get; set; }
}
