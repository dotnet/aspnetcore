// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Endpoints;

// Fills RazorComponentsServiceOptions.CacheBoundaryHybridCache from a registered HybridCache when the
// option was not configured explicitly, mirroring the circuit persistence DefaultHybridCache. An explicit
// CacheBoundaryHybridCache always takes precedence; auto-detection only provides the default.
internal sealed class DefaultCacheBoundaryHybridCache : IPostConfigureOptions<RazorComponentsServiceOptions>
{
    private readonly HybridCache? _hybridCache;

    public DefaultCacheBoundaryHybridCache(HybridCache? hybridCache = null)
    {
        _hybridCache = hybridCache;
    }

    public void PostConfigure(string? name, RazorComponentsServiceOptions options)
    {
        if (options.CacheBoundaryHybridCache is null && _hybridCache is not null)
        {
            options.CacheBoundaryHybridCache = _hybridCache;
        }
    }
}
