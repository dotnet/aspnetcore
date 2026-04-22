// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// A marker component that signals the renderer to not cache the rendered HTML output
/// of its child content. This is useful for opt-out scenarios when a parent component
/// enables caching but certain child content should be excluded.
/// </summary>
[CacheBoundaryPolicy(Excluded = true)]
public sealed class NotCacheBoundary : ComponentBase
{
    /// <summary>
    /// Gets or sets the content not to be cached.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc/>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(0, ChildContent);
    }
}
