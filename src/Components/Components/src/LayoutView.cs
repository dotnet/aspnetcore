// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components.Rendering;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Displays the specified content inside the specified layout and any further
/// nested layouts.
/// </summary>
public class LayoutView : IComponent
{
    private static readonly RenderFragment EmptyRenderFragment = builder => { };

    private RenderHandle _renderHandle;

    /// <summary>
    /// Gets or sets the content to display.
    /// </summary>
    [Parameter]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>
    /// Gets or sets the type of the layout in which to display the content.
    /// The type must implement <see cref="IComponent"/> and accept a parameter named <see cref="LayoutComponentBase.Body"/>.
    /// </summary>
    [Parameter]
    [DynamicallyAccessedMembers(Component)]
    public Type Layout { get; set; } = default!;

    /// <inheritdoc />
    public void Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
    }

    /// <inheritdoc />
    public Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);
        Render();
        return Task.CompletedTask;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Layout components are preserved because the LayoutAttribute constructor parameter is correctly annotated.")]
    private void Render()
    {
        // In the middle goes the supplied content
        var fragment = ChildContent ?? EmptyRenderFragment;

        // Then repeatedly wrap that in each layer of nested layout until we get
        // to a layout that has no parent
        var layoutType = Layout;
        while (layoutType != null)
        {
            fragment = WrapInLayout(layoutType, fragment);
            layoutType = GetParentLayoutType(layoutType);
        }

        _renderHandle.Render(fragment);
    }

    private static RenderFragment WrapInLayout([DynamicallyAccessedMembers(Component)] Type layoutType, RenderFragment bodyParam)
    {
        void Render(RenderTreeBuilder builder)
        {
            builder.OpenComponent(0, layoutType);
            builder.AddComponentParameter(1, LayoutComponentBase.BodyPropertyName, bodyParam);
            builder.CloseComponent();
        };

        return Render;
    }

    private static Type? GetParentLayoutType(Type type)
        => type.GetCustomAttribute<LayoutAttribute>()?.LayoutType;
}
