// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable warnings

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Displays the specified page component, rendering it inside its layout
/// and any further nested layouts.
/// </summary>
public class RouteView : IComponent
{
    private readonly RenderFragment _renderDelegate;
    private readonly RenderFragment _renderPageWithParametersDelegate;
    private RenderHandle _renderHandle;

    [Inject]
    private NavigationManager NavigationManager { get; set; }

    /// <summary>
    /// Gets or sets the route data. This determines the page that will be
    /// displayed and the parameter values that will be supplied to the page.
    /// </summary>
    [Parameter]
    [EditorRequired]
    public RouteData RouteData { get; set; }

    /// <summary>
    /// Gets or sets the type of a layout to be used if the page does not
    /// declare any layout. If specified, the type must implement <see cref="IComponent"/>
    /// and accept a parameter named <see cref="LayoutComponentBase.Body"/>.
    /// </summary>
    [Parameter]
    public Type DefaultLayout { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="RouteView"/>.
    /// </summary>
    public RouteView()
    {
        // Cache the delegate instances
        _renderDelegate = Render;
        _renderPageWithParametersDelegate = RenderPageWithParameters;
    }

    /// <inheritdoc />
    public void Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
    }

    /// <inheritdoc />
    public Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (RouteData == null)
        {
            throw new InvalidOperationException($"The {nameof(RouteView)} component requires a non-null value for the parameter {nameof(RouteData)}.");
        }

        _renderHandle.Render(_renderDelegate);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Renders the component.
    /// </summary>
    /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
    [UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "Layout components are preserved because the LayoutAttribute constructor parameter is correctly annotated.")]
    [UnconditionalSuppressMessage("Trimming", "IL2118", Justification = "Layout components are preserved because the LayoutAttribute constructor parameter is correctly annotated.")]
    protected virtual void Render(RenderTreeBuilder builder)
    {
        var pageLayoutType = RouteData.PageType.GetCustomAttribute<LayoutAttribute>()?.LayoutType
            ?? DefaultLayout;

        builder.OpenComponent<LayoutView>(0);
        builder.AddAttribute(1, nameof(LayoutView.Layout), pageLayoutType);
        builder.AddAttribute(2, nameof(LayoutView.ChildContent), _renderPageWithParametersDelegate);
        builder.CloseComponent();
    }

    private void RenderPageWithParameters(RenderTreeBuilder builder)
    {
        builder.OpenComponent(0, RouteData.PageType);

        foreach (var kvp in RouteData.RouteValues)
        {
            builder.AddAttribute(1, kvp.Key, kvp.Value);
        }

        var queryParameterSupplier = QueryParameterValueSupplier.ForType(RouteData.PageType);
        if (queryParameterSupplier is not null)
        {
            // Since this component does accept some parameters from query, we must supply values for all of them,
            // even if the querystring in the URI is empty. So don't skip the following logic.
            var url = NavigationManager.Uri;
            ReadOnlyMemory<char> query = default;
            var queryStartPos = url.IndexOf('?');
            if (queryStartPos >= 0)
            {
                var queryEndPos = url.IndexOf('#', queryStartPos);
                query = url.AsMemory(queryStartPos..(queryEndPos < 0 ? url.Length : queryEndPos));
            }
            queryParameterSupplier.RenderParametersFromQueryString(builder, query);
        }

        builder.CloseComponent();
    }
}
