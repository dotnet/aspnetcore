// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using Microsoft.AspNetCore.Components.Binding;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Defines the binding context for data bound from external sources.
/// </summary>
public sealed class CascadingModelBinder : SupplyParameterFromFormValueProvider, IComponent
{
    private RenderHandle _handle;
    private bool _hasPendingQueuedRender;

    /// <summary>
    /// The binding context name.
    /// </summary>
    [Parameter] public string Name { get; set; } = "";

    /// <summary>
    /// Specifies the content to be rendered inside this <see cref="CascadingModelBinder"/>.
    /// </summary>
    [Parameter] public RenderFragment<ModelBindingContext> ChildContent { get; set; } = default!;

    [CascadingParameter] ModelBindingContext? ParentContext { get; set; }

    [Inject] internal NavigationManager Navigation { get; set; } = null!;

    [Inject] internal IFormValueSupplier FormValueSupplier { get; set; } = null!;

    void IComponent.Attach(RenderHandle renderHandle)
    {
        _handle = renderHandle;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);
        if (ParentContext != null && string.IsNullOrEmpty(Name))
        {
            throw new InvalidOperationException($"Nested binding contexts must define a Name. (Parent context) = '{ParentContext.Name}'.");
        }

        if (BindingContext is null)
        {
            UpdateBindingInformation(FormValueSupplier, Navigation, ParentContext, Name);
        }

        Render();

        return Task.CompletedTask;
    }

    private void Render()
    {
        if (_hasPendingQueuedRender)
        {
            return;
        }
        _hasPendingQueuedRender = true;
        _handle.Render(builder =>
        {
            _hasPendingQueuedRender = false;
            builder.AddContent(0, ChildContent, BindingContext!);
        });
    }
}
