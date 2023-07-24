// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Defines the mapping scope for data received from form posts.
/// </summary>
public sealed class FormMappingScope : ICascadingValueSupplier, IComponent
{
    private SupplyParameterFromFormValueProvider? _cascadingValueSupplier;
    private RenderHandle _handle;
    private bool _hasPendingQueuedRender;

    /// <summary>
    /// The mapping scope name.
    /// </summary>
    [Parameter, EditorRequired] public string Name { get; set; } = default!;

    /// <summary>
    /// Specifies the content to be rendered inside this <see cref="FormMappingScope"/>.
    /// </summary>
    [Parameter] public RenderFragment<FormMappingContext> ChildContent { get; set; } = default!;

    [Inject] internal IFormValueMapper? FormValueModelBinder { get; set; } // Nonnull only on platforms that support HTTP form posts

    void IComponent.Attach(RenderHandle renderHandle)
    {
        _handle = renderHandle;
    }

    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (_cascadingValueSupplier is null)
        {
            if (string.IsNullOrEmpty(Name))
            {
                throw new InvalidOperationException($"The {nameof(FormMappingScope)} component requires a nonempty {nameof(Name)} parameter value.");
            }
            else if (Name.StartsWith('['))
            {
                // We use "scope-qualified form name starts with [" as a signal that there's a nonempty scope, so don't let the name itself start that way
                // Alternatively we could avoid packing both the scope and form name into a single string, or use some encoding. However it's very unlikely
                // this restriction will affect anyone, and the exact representation is an internal implementation detail.
                throw new InvalidOperationException($"The mapping scope name '{Name}' starts with a disallowed character.");
            }

            _cascadingValueSupplier = new SupplyParameterFromFormValueProvider(FormValueModelBinder, Name);
        }
        else if (!string.Equals(Name, _cascadingValueSupplier.MappingScopeName))
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
