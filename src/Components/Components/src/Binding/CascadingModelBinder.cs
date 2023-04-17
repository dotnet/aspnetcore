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
    /// The binding context name.
    /// </summary>
    [Parameter] public string BindingContextId { get; set; } = default!;

    /// <summary>
    /// Specifies the content to be rendered inside this <see cref="CascadingModelBinder"/>.
    /// </summary>
    [Parameter] public RenderFragment<ModelBindingContext> ChildContent { get; set; } = default!;

    [CascadingParameter] ModelBindingContext? ParentContext { get; set; }

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
            if (ParentContext != null && string.IsNullOrEmpty(Name))
            {
                throw new InvalidOperationException("Nested binding contexts must define a Name.");
            }

            var name = string.IsNullOrEmpty(ParentContext?.Name) ? Name : $"{ParentContext.Name}.{Name}";
            var bindingId = !string.IsNullOrEmpty(name) ? null : BindingContextId;
            _bindingContext = new ModelBindingContext(name, bindingId);

            _handle.Render(builder =>
            {
                builder.OpenComponent<CascadingValue<ModelBindingContext>>(0);
                builder.AddComponentParameter(1, nameof(CascadingValue<ModelBindingContext>.IsFixed), true);
                builder.AddComponentParameter(2, nameof(CascadingValue<ModelBindingContext>.Value), _bindingContext);
                builder.AddComponentParameter(3, nameof(CascadingValue<ModelBindingContext>.ChildContent), ChildContent?.Invoke(_bindingContext));
                builder.CloseComponent();
            });
        }

        return Task.CompletedTask;
    }
}
