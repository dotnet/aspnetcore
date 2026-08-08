// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable warnings

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Displays the specified page component, rendering it inside its layout
/// and any further nested layouts.
/// </summary>
public class RouteView : IComponent
{
    private RenderHandle _renderHandle;
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

    /// <inheritdoc />
    public void Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
    }

    /// <inheritdoc />
    public Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this, _renderHandle);

        if (RouteData == null)
        {
            throw new InvalidOperationException($"The {nameof(RouteView)} component requires a non-null value for the parameter {nameof(RouteData)}.");
        }

        _renderHandle.Render(Render);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Renders the component.
    /// </summary>
    /// <param name="builder">The <see cref="RenderTreeBuilder"/>.</param>
    [UnconditionalSuppressMessage("Trimming", "IL2110", Justification = "Layout components are resolved through ComponentTypeInfo.")]
    [UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "Layout components are resolved through ComponentTypeInfo.")]
    protected virtual void Render(RenderTreeBuilder builder)
    {
        var pageLayoutType = _renderHandle.ComponentTypeInfoResolver!
            .GetRequiredTypeInfo(RouteData.PageType)
            .Metadata
            .OfType<LayoutAttribute>()
            .FirstOrDefault()
            ?.LayoutType
            ?? DefaultLayout;

        builder.OpenComponent<LayoutView>(0);
        builder.AddComponentParameter(1, nameof(LayoutView.Layout), pageLayoutType);
        builder.AddComponentParameter(2, nameof(LayoutView.ChildContent), (RenderFragment)RenderPageWithParameters);
        builder.CloseComponent();
    }

    private void RenderPageWithParameters(RenderTreeBuilder builder)
    {
        builder.OpenComponent(0, RouteData.PageType);

        foreach (var kvp in RouteData.RouteValues)
        {
            builder.AddComponentParameter(1, kvp.Key, kvp.Value);
        }

        builder.CloseComponent();
    }
}
