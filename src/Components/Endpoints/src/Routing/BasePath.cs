// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Endpoints;

/// <summary>
/// Renders a &lt;base&gt; element whose <c>href</c> value matches the current request path base.
/// </summary>
public sealed class BasePath : IComponent
{
    private RenderHandle _renderHandle;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    void IComponent.Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        _renderHandle.Render(Render);
        return Task.CompletedTask;
    }

    private void Render(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "base");
        builder.AddAttribute(1, "href", ComputeHref());
        builder.CloseElement();
    }

    private string ComputeHref()
    {
        var baseUri = NavigationManager.BaseUri;
        if (Uri.TryCreate(baseUri, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.AbsolutePath;
        }

        return "/";
    }
}
