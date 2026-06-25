// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// A component that caches the rendered HTML of its child content during
/// server-side rendering (SSR). On cache hit, child components are not
/// instantiated or rendered.
/// </summary>
public sealed class CacheBoundary : IComponent, IDisposable
{
    private RenderHandle _renderHandle;

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
    /// Not supported when the cache boundary store uses <c>HybridCache</c>.
    /// </summary>
    [Parameter]
    public TimeSpan? ExpiresSliding { get; set; }

    /// <summary>
    /// Gets or sets a comma-separated list of query string parameter names to vary the cache by.
    /// Use <c>"*"</c> to vary by all query string parameters.
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

    [Inject] internal CacheBoundaryService? CacheService { get; set; }
    [CascadingParameter] internal HttpContext? HttpContext { get; set; }
    internal Func<string>? TreePositionKeyFactory { get; set; }
    internal string? TreePositionKey => TreePositionKeyFactory?.Invoke();

    internal bool IsInStreamingContext { get; set; }

    // The per-render coordination state produced by CacheBoundaryService. Null when caching is inactive
    // for this render; the renderer reads it to drive capture.
    internal CacheBoundaryRenderState? RenderState { get; private set; }

    /// <inheritdoc/>
    void IComponent.Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
    }

    /// <inheritdoc/>
    async Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        RenderState = HttpContext is { } httpContext && CacheService is { } cacheService
            ? await cacheService.PrepareAsync(this, httpContext)
            : null;

        _renderHandle.Render(BuildRenderTree);
    }

    private void BuildRenderTree(RenderTreeBuilder builder)
    {
        var content = RenderState?.Content ?? ChildContent;
        content?.Invoke(builder);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (RenderState is { } state)
        {
            CacheService?.OnBoundaryDisposed(state);
        }
    }
}
