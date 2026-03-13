// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// TODO
/// </summary>
public class CacheComponent : ComponentBase, IAsyncDisposable
{
    [Inject]
    private ICacheComponentStore Store { get; set; } = default!;

    [Inject]
    private IServiceProvider ServiceProvider { get; set; } = default!;

    [Inject]
    private ILoggerFactory LoggerFactory { get; set; } = default!;

    private string? _cachedHtml;

    /// <summary>
    /// TODO
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// TODO
    /// </summary>
    [Parameter]
    public string CacheKey { get; set; } = string.Empty;

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        var cacheKey = CacheKey ?? GetCacheComponentKey();

        if (Store.TryGetValue(cacheKey, out var cached))
        {
            _cachedHtml = cached;
            return;
        }

        if (ChildContent is null)
        {
            _cachedHtml = string.Empty;
            return;
        }

        var html = await ChildContent.ToHtmlAsync(ServiceProvider, LoggerFactory);

        _cachedHtml = html;
        Store.Set(cacheKey, html, TimeSpan.FromSeconds(10));
    }

    /// <inheritdoc/>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (_cachedHtml is not null)
        {
            builder.AddMarkupContent(0, _cachedHtml);
        }
    }

    private string GetCacheComponentKey()
    {
        if (ChildContent is null)
        {
            return $"CacheComponent:{GetType().FullName}:empty";
        }

        return $"CacheComponent:{ChildContent.Method.DeclaringType!.FullName}.{ChildContent.Method.Name}";
    }
}
