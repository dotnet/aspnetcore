// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using Microsoft.AspNetCore.Components.Forms.ModelBinding;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Defines the binding context for data bound from external sources.
/// </summary>
public sealed class CascadingModelBinder : ICascadingValueSupplier, IComponent
{
    private ICascadingValueSupplier? _cascadingValueSupplier;
    private ModelBindingContext? _bindingContext;
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

    [CascadingParameter] private ModelBindingContext? ParentContext { get; set; }

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

        if (_cascadingValueSupplier is null)
        {
            var cascadingValueSupplier = new SupplyParameterFromFormValueProvider(FormValueSupplier, Navigation, ParentContext, Name);
            _cascadingValueSupplier = cascadingValueSupplier;
            _bindingContext = cascadingValueSupplier.BindingContext;
        }

        if (!_hasPendingQueuedRender)
        {
            _hasPendingQueuedRender = true;
            _handle.Render(BuildRenderTree);
        }

        return Task.CompletedTask;
    }

    private void BuildRenderTree(RenderTreeBuilder builder)
    {
        _hasPendingQueuedRender = false;
        builder.AddContent(0, ChildContent, _bindingContext!);
    }

    // The implementation of ICascadingValueSupplier won't be used until this component is rendered,
    // because it's only used by descendant components. So we know _cascadingValueSupplier will be
    // nonnull by that time.

    bool ICascadingValueSupplier.IsFixed
        => _cascadingValueSupplier!.IsFixed;

    bool ICascadingValueSupplier.CanSupplyValue(in CascadingParameterInfo parameterInfo)
        => _cascadingValueSupplier!.CanSupplyValue(parameterInfo);

    object? ICascadingValueSupplier.GetCurrentValue(in CascadingParameterInfo parameterInfo)
        => _cascadingValueSupplier!.GetCurrentValue(in parameterInfo);

    void ICascadingValueSupplier.Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
        => throw new NotSupportedException();

    void ICascadingValueSupplier.Unsubscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
        => throw new NotSupportedException();
}
