// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using Microsoft.AspNetCore.Components.Binding;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Defines the binding context for data bound from external sources.
/// </summary>
public class CascadingModelBinder : IComponent
{
    private RenderHandle _handle;
    private bool _hasRendered;
    private ModelBindingContext? _bindingContext;

    /// <summary>
    /// The binding context name.
    /// </summary>
    [Parameter] public string Name { get; set; } = default!;

    /// <summary>
    /// Specifies the content to be rendered inside this <see cref="CascadingModelBinder"/>.
    /// </summary>
    [Parameter] public RenderFragment ChildContent { get; set; } = default!;

    void IComponent.Attach(RenderHandle renderHandle)
    {
        _handle = renderHandle;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        if (!_hasRendered)
        {
            _hasRendered = true;
            parameters.SetParameterProperties(this);

            _bindingContext = new ModelBindingContext(Name);
            _handle.Render(builder =>
            {
                builder.OpenComponent<CascadingValue<ModelBindingContext>>(0);
                builder.AddComponentParameter(1, nameof(CascadingValue<ModelBindingContext>.IsFixed), true);
                builder.AddComponentParameter(2, nameof(CascadingValue<ModelBindingContext>.Value), _bindingContext);
                builder.AddComponentParameter(3, nameof(CascadingValue<ModelBindingContext>.ChildContent), ChildContent);
                builder.CloseComponent();
            });
        }

        return Task.CompletedTask;
    }
}
