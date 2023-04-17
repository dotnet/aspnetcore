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
    private ModelBindingContext? _bindingContext;

    /// <summary>
    /// The binding context name.
    /// </summary>
    [Parameter] public string Name { get; set; } = default!;

    /// <summary>
    /// The binding context name.
    /// </summary>
    [Parameter] public bool IsFixed { get; set; }

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
        parameters.SetParameterProperties(this);
        if (ParentContext != null && string.IsNullOrEmpty(Name))
        {
            throw new InvalidOperationException($"Nested binding contexts must define a Name. (Parent context) = '{ParentContext.BindingContextId}'.");
        }

        var name = string.IsNullOrEmpty(ParentContext?.Name) ? Name : $"{ParentContext.Name}.{Name}";
        var bindingId = !string.IsNullOrEmpty(name) ? null : BindingContextId;
        var bindingContext = _bindingContext != null &&
            string.Equals(_bindingContext.Name, Name, StringComparison.Ordinal) &&
            string.Equals(_bindingContext.BindingContextId, BindingContextId, StringComparison.Ordinal) ?
            _bindingContext : new ModelBindingContext(name, bindingId);
        if (IsFixed && _bindingContext != null && _bindingContext != bindingContext)
        {
            // Throw an exception if either the Name or the BindingContextId changed. Once a CascadingModelBinder has been initialized
            // as fixed, it can't change it's name nor its BindingContextId. This can happen in several situations:
            // * Component ParentContext hierarchy changes.
            //   * Technically, the component won't be retained in this case and will be destroyed instead.
            // * A parent changes Name.
            // * A parent changes BindingContextId.
            throw new InvalidOperationException($"'{nameof(CascadingModelBinder)}' 'Name' and 'BindingContextId' can't change after initialized.");
        }

        // It doesn't matter that we don't check IsFixed, since the CascadingValue we are setting up will throw if the app changes.
        _bindingContext = bindingContext;
        _handle.Render(builder =>
        {
            builder.OpenComponent<CascadingValue<ModelBindingContext>>(0);
            builder.AddComponentParameter(1, nameof(CascadingValue<ModelBindingContext>.IsFixed), IsFixed);
            builder.AddComponentParameter(2, nameof(CascadingValue<ModelBindingContext>.Value), _bindingContext);
            builder.AddComponentParameter(3, nameof(CascadingValue<ModelBindingContext>.ChildContent), ChildContent?.Invoke(_bindingContext));
            builder.CloseComponent();
        });

        return Task.CompletedTask;
    }
}
