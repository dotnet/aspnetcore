// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// A component that caches the rendered HTML output of its child content during
/// server-side rendering (SSR). On cache hit, child components are not instantiated
/// or rendered, preventing unnecessary data fetching and computation.
/// </summary>
public sealed class CacheComponent : ComponentBase
{
    /// <summary>
    /// Gets or sets the content to be cached.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets an explicit cache key for additional disambiguation. Only needed
    /// when a reusable component uses <see cref="CacheComponent"/> internally and
    /// multiple instances of that component appear on the same page.
    /// </summary>
    [Parameter]
    public string? CacheKey { get; set; }

    /// <summary>
    /// Gets or sets whether caching is enabled. Defaults to <c>true</c>.
    /// </summary>
    [Parameter]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the duration, from the time the cache entry was added, when it should be evicted.
    /// </summary>
    [Parameter]
    public TimeSpan? ExpiresAfter { get; set; }

    /// <summary>
    /// Gets or sets the exact <see cref="DateTimeOffset"/> the cache entry should be evicted.
    /// </summary>
    [Parameter]
    public DateTimeOffset? ExpiresOn { get; set; }

    /// <summary>
    /// Gets or sets the duration from last access that the cache entry should be evicted.
    /// </summary>
    [Parameter]
    public TimeSpan? ExpiresSliding { get; set; }

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

    // Injected cache store — registered as singleton in DI.
    [Inject] internal CacheComponentStore? CacheStore { get; set; }

    // HttpContext is cascaded by the SSR infrastructure.
    [CascadingParameter] internal HttpContext? HttpContext { get; set; }

    internal string? ResolvedCacheKey { get; private set; }
    internal string? CachedData { get; private set; }

    internal CacheComponentVaryBy GetVaryByOptions() => new()
    {
        VaryByQuery = VaryByQuery is not null,
        VaryByRoute = VaryByRoute is not null,
        VaryByHeader = VaryByHeader is not null,
        VaryByCookie = VaryByCookie is not null,
        VaryByUser = VaryByUser is true,
        VaryByCulture = VaryByCulture is true,
    };

    /// <inheritdoc/>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Enabled && CacheStore is not null && HttpContext is { } httpContext)
        {
            ResolvedCacheKey = CacheComponentKeyResolver.ComputeKey(this, httpContext);
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
                    var renderMode = segment.ReconstructRenderMode();
                    if (renderMode is not null)
                    {
                        builder.AddComponentRenderMode(renderMode);
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

    private bool TryRestoreFromCache(out CacheComponentJson? cacheJson)
    {
        cacheJson = null;

        if (string.IsNullOrEmpty(CachedData))
        {
            return false;
        }

        try
        {
            cacheJson = CacheComponentJson.Deserialize(CachedData);

            return cacheJson.Count > 0;
        }
        catch (Exception ex)
        {
            HttpContext?.RequestServices.GetService<ILoggerFactory>()
                ?.CreateLogger<CacheComponent>()
                .LogWarning(ex, "Failed to restore CacheComponent from cached data. Falling back to fresh render.");
            return false;
        }
    }
}
