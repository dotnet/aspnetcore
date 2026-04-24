// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
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

    internal CacheBoundaryVaryBy GetVaryByOptions()
    {
        var result = CacheBoundaryVaryBy.None;

        if (VaryByQuery is not null)
        {
            result |= CacheBoundaryVaryBy.Query;
        }

        if (VaryByRoute is not null)
        {
            result |= CacheBoundaryVaryBy.Route;
        }

        if (VaryByHeader is not null)
        {
            result |= CacheBoundaryVaryBy.Header;
        }

        if (VaryByCookie is not null)
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

        if (!TryRestoreFromCache(out var cacheJson))
        {
            CachedData = null;
            builder.AddContent(0, ChildContent);
            return;
        }

        using var scratchBuilder = new RenderTreeBuilder();
        ArrayRange<RenderTreeFrame> freshFrames = default;
        if (ChildContent is not null)
        {
            ChildContent(scratchBuilder);
            freshFrames = scratchBuilder.GetFrames();
        }

        int seq = 0;
        int freshFrameSearchStart = 0;
        foreach (var segment in cacheJson!)
        {
            switch (segment.Kind)
            {
                case CacheSegmentKind.Html:
                    builder.AddMarkupContent(seq++, segment.Html);
                    break;

                case CacheSegmentKind.Hole:
                    builder.OpenComponent(seq++, segment.ComponentType!);
                    if (segment.ComponentKey is not null)
                    {
                        builder.SetKey(segment.ComponentKey);
                    }
                    freshFrameSearchStart = ApplyFreshAttributes(
                        builder, ref seq, freshFrames, freshFrameSearchStart, segment.ComponentType!);
                    if (segment.RenderModeName is { } renderModeName)
                    {
                        builder.AddComponentRenderMode(renderModeName switch
                        {
                            "InteractiveServer" => Web.RenderMode.InteractiveServer,
                            "InteractiveWebAssembly" => Web.RenderMode.InteractiveWebAssembly,
                            "InteractiveAuto" => Web.RenderMode.InteractiveAuto,
                            _ => throw new InvalidOperationException($"Unknown cached render mode: '{renderModeName}'."),
                        });
                    }
                    builder.CloseComponent();
                    break;
            }
        }
    }

    private static int ApplyFreshAttributes(
        RenderTreeBuilder builder, ref int seq,
        ArrayRange<RenderTreeFrame> freshFrames, int searchStart, Type componentType)
    {
        for (var i = searchStart; i < freshFrames.Count; i++)
        {
            ref var frame = ref freshFrames.Array[i];
            if (frame.FrameType != RenderTreeFrameType.Component || frame.ComponentType != componentType)
            {
                continue;
            }

            for (var j = i + 1; j < freshFrames.Count; j++)
            {
                ref var attrFrame = ref freshFrames.Array[j];
                if (attrFrame.FrameType != RenderTreeFrameType.Attribute)
                {
                    break;
                }
                builder.AddComponentParameter(seq++, attrFrame.AttributeName, attrFrame.AttributeValue);
            }
            return i + 1;
        }
        return searchStart;
    }

    private bool TryRestoreFromCache(out CacheBoundaryJson? cacheJson)
    {
        cacheJson = null;

        if (string.IsNullOrEmpty(CachedData))
        {
            return false;
        }

        try
        {
            cacheJson = CacheBoundaryJson.Deserialize(CachedData);
            return cacheJson.Count > 0;
        }
        catch (Exception ex)
        {
            HttpContext?.RequestServices.GetService<ILoggerFactory>()
                ?.CreateLogger<CacheBoundary>()
                .LogWarning(ex, "Failed to restore CacheBoundary from cached data. Falling back to fresh render.");
            return false;
        }
    }
}
