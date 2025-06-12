// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints;

internal class ResourcePreloadService
{
    private Action<List<PreloadAsset>>? handler;

    public void SetPreloadingHandler(Action<List<PreloadAsset>> handler)
        => this.handler = handler;

    public void Preload(List<PreloadAsset> assets)
        => this.handler?.Invoke(assets);
}
