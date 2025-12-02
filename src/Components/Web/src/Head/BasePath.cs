// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Web;

/// <summary>
/// Renders a &lt;base&gt; element whose <c>href</c> value matches the current request path base.
/// </summary>
public sealed class BasePath : ComponentBase
{
    private string _href = "/";

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

    /// <summary>
    /// Gets or sets an explicit base path to render. When provided, this value takes precedence
    /// over values inferred from the current request or fallback parameters.
    /// </summary>
    [Parameter]
    public string? Href { get; set; }

    /// <summary>
    /// Gets or sets the fallback <c>href</c> value to use when the current request information isn't available.
    /// </summary>
    [Parameter]
    public string? FallbackHref { get; set; }

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
        if (!string.IsNullOrEmpty(Href))
        {
            return Normalize(Href);
        }

        if (TryGetPathBase(out var pathBase))
        {
            return Normalize(pathBase);
        }

        if (!string.IsNullOrEmpty(FallbackHref))
        {
            return Normalize(FallbackHref);
        }

        var baseUri = NavigationManager.BaseUri;
        if (Uri.TryCreate(baseUri, UriKind.Absolute, out var absoluteUri))
        {
            return Normalize(absoluteUri.AbsolutePath);
        }

        return "/";
    }

    private bool TryGetPathBase([NotNullWhen(true)] out string? pathBase)
    {
        var accessor = Services.GetService<IHttpContextAccessor>();
        var contextPathBase = accessor?.HttpContext?.Request.PathBase;
        if (contextPathBase.HasValue)
        {
            pathBase = contextPathBase.Value;
            return true;
        }

        pathBase = null;
        return false;
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "/";
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var absoluteUri))
        {
            value = absoluteUri.AbsolutePath;
        }

        if (!value.StartsWith('/'))
        {
            value = "/" + value;
        }

        return value.EndsWith('/') ? value : value + "/";
    }
}
