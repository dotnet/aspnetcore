// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Defines the mapping context for data received from form posts.
/// </summary>
public sealed class FormMappingScope : ICascadingValueSupplier, IComponent
{
    private SupplyParameterFromFormValueProvider? _cascadingValueSupplier;
    private RenderHandle _handle;
    private bool _hasPendingQueuedRender;

    /// <summary>
    /// The mapping context name.
    /// </summary>
    [Parameter] public string Name { get; set; } = "";

    /// <summary>
    /// Specifies the content to be rendered inside this <see cref="FormMappingScope"/>.
    /// </summary>
    [Parameter] public RenderFragment<FormMappingContext> ChildContent { get; set; } = default!;

    [CascadingParameter] private FormMappingContext? ParentContext { get; set; }

    [Inject] internal NavigationManager Navigation { get; set; } = null!;

    [Inject] internal IFormValueMapper? FormValueModelBinder { get; set; } // Nonnull only on platforms that support HTTP form posts

    void IComponent.Attach(RenderHandle renderHandle)
    {
        _handle = renderHandle;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);
        if (ParentContext != null && string.IsNullOrEmpty(Name))
        {
            throw new InvalidOperationException($"Nested form mapping contexts must define a Name. (Parent context) = '{ParentContext.Name}'.");
        }

        if (_cascadingValueSupplier is null)
        {
            _cascadingValueSupplier = new SupplyParameterFromFormValueProvider(FormValueModelBinder, Navigation, ParentContext, Name);
        }
        else if (!string.Equals(Name, _cascadingValueSupplier.Name))
        {
            throw new InvalidOperationException($"{nameof(FormMappingScope)} '{nameof(Name)}' can't change after initialization.");
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
        builder.AddContent(0, ChildContent, _cascadingValueSupplier!.MappingContext);
    }

    // The implementation of ICascadingValueSupplier won't be used until this component is rendered,
    // because it's only used by descendant components. So we know _cascadingValueSupplier will be
    // nonnull by that time.

    bool ICascadingValueSupplier.IsFixed
        => true;

    bool ICascadingValueSupplier.CanSupplyValue(in CascadingParameterInfo parameterInfo)
        => _cascadingValueSupplier!.CanSupplyValue(parameterInfo);

    object? ICascadingValueSupplier.GetCurrentValue(in CascadingParameterInfo parameterInfo)
        => _cascadingValueSupplier!.GetCurrentValue(in parameterInfo);

    void ICascadingValueSupplier.Subscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
        => throw new NotSupportedException();

    void ICascadingValueSupplier.Unsubscribe(ComponentState subscriber, in CascadingParameterInfo parameterInfo)
        => throw new NotSupportedException();
}
