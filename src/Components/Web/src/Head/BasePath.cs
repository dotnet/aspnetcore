// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Renders a &lt;base&gt; element whose <c>href</c> value matches the current request path base.
/// </summary>
public sealed class BasePath : ComponentBase
{
    private string _href = "/";

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _href = ComputeHref();
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "base");
        builder.AddAttribute(1, "href", _href);
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
