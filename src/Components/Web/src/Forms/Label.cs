// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Renders a <c>&lt;label&gt;</c> element for a specified field, reading the display name from
/// <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute"/> or
/// <see cref="System.ComponentModel.DisplayNameAttribute"/> if present, or falling back to the property name.
/// The label wraps its child content (typically an input component), providing implicit association
/// without requiring matching for/id attributes.
/// </summary>
/// <typeparam name="TValue">The type of the field.</typeparam>
public class Label<TValue> : IComponent
{
    private RenderHandle _renderHandle;
    private Expression<Func<TValue>>? _previousFieldAccessor;
    private string? _displayName;

    /// <summary>
    /// Specifies the field for which the label should be rendered.
    /// </summary>
    [Parameter, EditorRequired]
    public Expression<Func<TValue>>? For { get; set; }

    /// <summary>
    /// Gets or sets the child content to be rendered inside the label element.
    /// Typically this contains an input component that will be implicitly associated with the label.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets a collection of additional attributes that will be applied to the label element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <inheritdoc />
    void IComponent.Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
    }

    /// <inheritdoc />
    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        var previousChildContent = ChildContent;
        var previousAdditionalAttributes = AdditionalAttributes;

        parameters.SetParameterProperties(this);

        if (For is null)
        {
            throw new InvalidOperationException($"{GetType()} requires a value for the " +
                $"{nameof(For)} parameter.");
        }

        // Only recalculate display name if the expression changed
        if (For != _previousFieldAccessor)
        {
            var newDisplayName = ExpressionMemberAccessor.GetDisplayName(For);

            if (newDisplayName != _displayName)
            {
                _displayName = newDisplayName;
                _renderHandle.Render(BuildRenderTree);
            }

            _previousFieldAccessor = For;
        }
        else if (ChildContent != previousChildContent || AdditionalAttributes != previousAdditionalAttributes)
        {
            // Re-render if other parameters changed
            _renderHandle.Render(BuildRenderTree);
        }

        return Task.CompletedTask;
    }

    private void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "label");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddContent(2, _displayName);
        builder.AddContent(3, ChildContent);
        builder.CloseElement();
    }
}
