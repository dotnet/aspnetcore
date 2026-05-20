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
public sealed class CacheBoundary : ComponentBase
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
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Enabled && CacheStore is not null && HttpContext is { } httpContext)
        {
            ResolvedCacheKey = CacheBoundaryKeyResolver.ComputeKey(this, httpContext);
            CachedData = CacheStore.Get(ResolvedCacheKey);
        }

        if (!TryRestoreFromCache(out var nodes))
        {
            CachedData = null;
            if (Enabled && CacheStore is not null && ChildContent is { } childContent)
            {
                ChildContentCapture = new RenderFragmentCapture(childContent);
                builder.AddContent(0, (RenderFragment)ChildContentCapture.Invoke);
            }
            else
            {
                ChildContentCapture = null;
                builder.AddContent(0, ChildContent);
            }
            return;
        }

        ChildContentCapture = null;

        // Cache hit: invoke the deserialized RenderFragment straight into the live builder.
        RenderFragmentSerializer.Deserialize(nodes!, _jsonOptions, _parametersTypeCache)(builder);
    }

    private ILogger? GetLogger()
        => HttpContext?.RequestServices.GetService<ILoggerFactory>()?.CreateLogger<CacheBoundary>();

    private bool TryRestoreFromCache(out List<RenderTreeNode>? nodes)
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
