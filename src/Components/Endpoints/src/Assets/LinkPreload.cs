// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Represents link elements for preloading assets.
/// </summary>
public sealed class LinkPreload : IComponent
{
    private RenderHandle renderHandle;
    private List<PreloadAsset>? assets;

    [Inject]
    internal ResourcePreloadService? Service { get; set; }

    void IComponent.Attach(RenderHandle renderHandle)
    {
        this.renderHandle = renderHandle;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        Service?.SetPreloadingHandler(PreloadAssets);
        renderHandle.Render(RenderPreloadAssets);
        return Task.CompletedTask;
    }

    private void PreloadAssets(List<PreloadAsset> assets)
    {
        if (this.assets != null)
        {
            return;
        }

        this.assets = assets;
        renderHandle.Render(RenderPreloadAssets);
    }

    private void RenderPreloadAssets(RenderTreeBuilder builder)
    {
        if (assets == null)
        {
            return;
        }

        for (var i = 0; i < assets.Count; i ++)
        {
            var asset = assets[i];
            builder.OpenElement(0, "link");
            builder.SetKey(assets[i]);
            builder.AddAttribute(1, "href", asset.Url);
            builder.AddAttribute(2, "rel", asset.PreloadRel);
            if (!string.IsNullOrEmpty(asset.PreloadAs))
            {
                builder.AddAttribute(3, "as", asset.PreloadAs);
            }
            if (!string.IsNullOrEmpty(asset.PreloadPriority))
            {
                builder.AddAttribute(4, "fetchpriority", asset.PreloadPriority);
            }
            if (!string.IsNullOrEmpty(asset.PreloadCrossorigin))
            {
                builder.AddAttribute(5, "crossorigin", asset.PreloadCrossorigin);
            }
            if (!string.IsNullOrEmpty(asset.Integrity))
            {
                builder.AddAttribute(6, "integrity", asset.Integrity);
            }
            builder.CloseElement();
        }
    }
}
